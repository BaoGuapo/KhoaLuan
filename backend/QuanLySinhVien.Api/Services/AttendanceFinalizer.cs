using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Services;

/// <summary>
/// Chốt điểm danh một buổi học: ghi Vắng cho sinh viên chưa có kết quả (hoặc Có mặt cả lớp khi tự chốt
/// buổi không được điểm danh), tính lại mức cảnh báo,
/// đóng phiên QR và gửi thư khi mức cảnh báo tăng. Dùng chung cho nút chốt và việc tự chốt khi hết giờ.
/// </summary>
public sealed class AttendanceFinalizer(AppDbContext db, AttendanceEmailNotifier notifier)
{
    /// <summary>
    /// Chốt buổi học; trả về false khi không tìm thấy buổi. Buổi đã chốt được bỏ qua.
    /// Khi tự chốt mà giảng viên không điểm danh gì cả buổi (không mở phiên QR, không ghi kết quả nào),
    /// cả lớp được tính có mặt thay vì vắng, để sinh viên không bị cảnh báo oan.
    /// </summary>
    public Task<bool> FinalizeAsync(string maBuoiHoc, CancellationToken ct, bool tuDong = false) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var session = await db.BuoiHocs
                .Include(x => x.LichHoc)
                .SingleOrDefaultAsync(x => x.MaBuoiHoc == maBuoiHoc, ct);
            if (session is null) return false;
            if (session.TrangThaiBuoiHoc == "DaChot") return true;

            var enrollments = await db.DanhSachSinhVienLops
                .Where(x => x.MaLopHocPhan == session.LichHoc.MaLopHocPhan)
                .ToListAsync(ct);
            var existingCodes = (await db.ChiTietDiemDanhs
                .Where(x => x.MaBuoiHoc == maBuoiHoc)
                .Select(x => x.MaSinhVien)
                .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var khongDiemDanh = tuDong && existingCodes.Count == 0
                && !await db.PhienDiemDanhs.AnyAsync(x => x.MaBuoiHoc == maBuoiHoc, ct);
            foreach (var enrollment in enrollments.Where(x => !existingCodes.Contains(x.MaSinhVien)))
            {
                db.ChiTietDiemDanhs.Add(new ChiTietDiemDanh
                {
                    MaBuoiHoc = maBuoiHoc,
                    MaSinhVien = enrollment.MaSinhVien,
                    TrangThai = khongDiemDanh ? "CoMat" : "Vang"
                });
            }

            session.TrangThaiBuoiHoc = "DaChot";
            session.ThoiGianDiemDanh ??= DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            var warningChanges = await RecalculateWarningsAsync(enrollments, ct);
            await db.SaveChangesAsync(ct);
            var openSessions = await db.PhienDiemDanhs.Where(x => x.MaBuoiHoc == maBuoiHoc && x.DongLucUtc == null).ToListAsync(ct);
            foreach (var openSession in openSessions) openSession.DongLucUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            // Chỉ xếp thư sau khi giao dịch đã commit, để không báo cảnh báo cho một lần chốt bị rollback.
            await notifier.NotifyAttendanceWarningsAsync(session.LichHoc.MaLopHocPhan, warningChanges, ct);
            return true;
        });

    /// <summary>Tính lại số buổi vắng và mức cảnh báo; trả về mức cũ/mới để báo email.</summary>
    public async Task<List<(string MaSinhVien, string MucCu, string MucMoi, int SoBuoiVang)>> RecalculateWarningsAsync(
        IEnumerable<DanhSachSinhVienLop> enrollments,
        CancellationToken ct)
    {
        var warningChanges = new List<(string MaSinhVien, string MucCu, string MucMoi, int SoBuoiVang)>();
        foreach (var enrollment in enrollments)
        {
            var mucCu = enrollment.TrangThaiCanhBao ?? "BinhThuong";
            enrollment.TongSoBuoiVang = await db.ChiTietDiemDanhs.CountAsync(x =>
                x.MaSinhVien == enrollment.MaSinhVien
                && x.BuoiHoc.LichHoc.MaLopHocPhan == enrollment.MaLopHocPhan
                && x.TrangThai == "Vang", ct);
            enrollment.TrangThaiCanhBao = enrollment.TongSoBuoiVang switch
            {
                >= 4 => "CamThi",
                >= 2 => "CanhBao",
                _ => "BinhThuong"
            };
            warningChanges.Add((enrollment.MaSinhVien, mucCu, enrollment.TrangThaiCanhBao, enrollment.TongSoBuoiVang));
        }
        return warningChanges;
    }

    /// <summary>
    /// Chốt mọi buổi đã hết giờ lên lớp mà chưa chốt. Buổi còn phiên QR đang mở được chờ tới khi
    /// phiên hết hạn, để sinh viên đang quét dở không bị cắt ngang.
    /// </summary>
    public async Task<int> FinalizeEndedAsync(DateTime utcNow, CancellationToken ct)
    {
        var bayGio = ClassPeriods.BayGioVietNam(utcNow);
        var homNay = bayGio.Date;
        var candidates = await db.BuoiHocs.AsNoTracking()
            .Where(x => x.TrangThaiBuoiHoc != "DaChot" && x.NgayHocCuThe <= homNay)
            .Where(x => !db.PhienDiemDanhs.Any(p => p.MaBuoiHoc == x.MaBuoiHoc && p.DongLucUtc == null && p.HetHanLucUtc > utcNow))
            .Select(x => new { x.MaBuoiHoc, x.NgayHocCuThe, x.LichHoc.TietBatDau, x.LichHoc.TietKetThuc })
            .ToListAsync(ct);
        var finalized = 0;
        foreach (var lesson in candidates)
        {
            // Lịch học khai báo tiết ngoài bảng giờ thì coi như kết thúc lúc hết ngày.
            var ketThuc = ClassPeriods.KhungGioDiemDanh(lesson.NgayHocCuThe, lesson.TietBatDau, lesson.TietKetThuc)?.DongLuc
                ?? lesson.NgayHocCuThe.Date.AddDays(1);
            if (bayGio < ketThuc) continue;
            if (await FinalizeAsync(lesson.MaBuoiHoc, ct, tuDong: true)) finalized++;
        }
        return finalized;
    }
}

/// <summary>Định kỳ tự chốt điểm danh các buổi học đã kết thúc.</summary>
public sealed class AutoFinalizeAttendanceService(
    IServiceScopeFactory scopes,
    ILogger<AutoFinalizeAttendanceService> logger) : BackgroundService
{
    private static readonly TimeSpan ChuKy = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(ChuKy);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var finalizer = scope.ServiceProvider.GetRequiredService<AttendanceFinalizer>();
                var count = await finalizer.FinalizeEndedAsync(DateTime.UtcNow, stoppingToken);
                if (count > 0) logger.LogInformation("Đã tự chốt điểm danh {Count} buổi học đã kết thúc.", count);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Lỗi một lượt (mất kết nối CSDL...) không được dừng tác vụ nền; lượt sau thử lại.
                logger.LogError(e, "Tự chốt điểm danh thất bại, sẽ thử lại sau {Minutes} phút.", ChuKy.TotalMinutes);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
