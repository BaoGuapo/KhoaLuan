using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Services;

/// <summary>Soạn nội dung các thư gửi cho sinh viên và lấy địa chỉ người nhận.</summary>
public sealed class AttendanceEmailNotifier(AppDbContext db, IEmailSender email)
{
    /// <summary>
    /// Thư báo có thông báo mới. Thông báo gắn với một lớp học phần chỉ gửi cho sinh viên lớp đó,
    /// thông báo chung gửi cho mọi sinh viên còn hoạt động.
    /// </summary>
    public async Task NotifyAnnouncementAsync(ThongBao notice, CancellationToken ct)
    {
        var recipients = notice.MaLopHocPhan is { Length: > 0 } maLopHocPhan
            ? await db.DanhSachSinhVienLops.AsNoTracking()
                .Where(x => x.MaLopHocPhan == maLopHocPhan && x.SinhVien.Email != null)
                .Select(x => x.SinhVien.Email!)
                .ToListAsync(ct)
            : await db.SinhViens.AsNoTracking()
                .Where(x => x.Email != null && x.TrangThaiTaiKhoan)
                .Select(x => x.Email!)
                .ToListAsync(ct);
        if (recipients.Count == 0) return;

        var phamVi = notice.MaLopHocPhan is { Length: > 0 } code ? $"Lớp học phần: {code}" : "Thông báo chung";
        email.Queue(new EmailMessage(recipients, $"[Thông báo] {notice.TieuDe}",
            $"""
             {notice.TieuDe}

             {phamVi}
             Hiệu lực từ: {notice.HieuLucTuUtc.AddHours(7):dd/MM/yyyy HH:mm}

             {notice.NoiDung}

             --
             Thư tự động từ hệ thống quản lý sinh viên. Vui lòng không trả lời thư này.
             """));
    }

    /// <summary>
    /// Thư cảnh báo chuyên cần, chỉ gửi cho sinh viên vừa bị nâng mức cảnh báo
    /// sau khi giảng viên chốt điểm danh, để không gửi lặp mỗi buổi.
    /// </summary>
    public async Task NotifyAttendanceWarningsAsync(
        string maLopHocPhan,
        IReadOnlyCollection<(string MaSinhVien, string MucCu, string MucMoi, int SoBuoiVang)> changes,
        CancellationToken ct)
    {
        var worsened = changes.Where(x => Rank(x.MucMoi) > Rank(x.MucCu)).ToList();
        if (worsened.Count == 0) return;

        var codes = worsened.Select(x => x.MaSinhVien).ToList();
        var students = await db.SinhViens.AsNoTracking()
            .Where(x => codes.Contains(x.MaSinhVien) && x.Email != null)
            .Select(x => new { x.MaSinhVien, x.HoTen, Email = x.Email! })
            .ToListAsync(ct);
        var courseName = await db.LopHocPhans.AsNoTracking()
            .Where(x => x.MaLopHocPhan == maLopHocPhan)
            .Select(x => x.MonHoc.TenMonHoc)
            .SingleOrDefaultAsync(ct) ?? maLopHocPhan;

        foreach (var student in students)
        {
            var change = worsened.Single(x => x.MaSinhVien == student.MaSinhVien);
            // Gửi riêng từng sinh viên vì nội dung chứa số buổi vắng của chính họ.
            email.Queue(new EmailMessage([student.Email],
                change.MucMoi == "CamThi"
                    ? $"[Cấm thi] {courseName} — {maLopHocPhan}"
                    : $"[Cảnh báo chuyên cần] {courseName} — {maLopHocPhan}",
                $"""
                 Chào {student.HoTen} ({student.MaSinhVien}),

                 Lớp học phần {maLopHocPhan} — {courseName}
                 Số buổi vắng: {change.SoBuoiVang}
                 Tình trạng: {Describe(change.MucMoi)}

                 {(change.MucMoi == "CamThi"
                     ? "Bạn đã vượt số buổi vắng cho phép và không đủ điều kiện dự thi học phần này. Liên hệ giảng viên hoặc phòng đào tạo nếu có lý do chính đáng."
                     : "Bạn cần hạn chế vắng mặt ở các buổi còn lại để đủ điều kiện dự thi.")}

                 --
                 Thư tự động từ hệ thống quản lý sinh viên. Vui lòng không trả lời thư này.
                 """));
        }
    }

    private static int Rank(string? level) => level switch
    {
        "CamThi" => 2,
        "CanhBao" => 1,
        _ => 0
    };

    private static string Describe(string level) => level switch
    {
        "CamThi" => "Cấm thi",
        "CanhBao" => "Cảnh báo chuyên cần",
        _ => "Bình thường"
    };
}
