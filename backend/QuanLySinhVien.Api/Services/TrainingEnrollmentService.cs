using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Services;

public sealed record EnrollmentResult(int StatusCode, string Message);
public sealed class TrainingEnrollmentService(AppDbContext db)
{
    public async Task<EnrollmentResult> EnrollAsync(string studentCode, string classCode, CancellationToken ct, string? registrationType = null) =>
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var student = await db.SinhViens.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == studentCode && x.TrangThaiTaiKhoan, ct);
            if (student is null) return new EnrollmentResult(404, "Không tìm thấy sinh viên đang hoạt động.");
            var course = await db.LopHocPhans.AsNoTracking().Include(x => x.MonHoc).SingleOrDefaultAsync(x => x.MaLopHocPhan == classCode, ct);
            if (course is null) return new EnrollmentResult(404, "Không tìm thấy lớp học phần.");
            if (course.TrangThai != "0") return new EnrollmentResult(409, "Lớp chưa mở đăng ký.");
            if (!await db.LichHocs.AnyAsync(x => x.MaLopHocPhan == classCode, ct)) return new EnrollmentResult(409, "Lớp chưa có lịch học.");
            var rules = await StudentRegistrationRules.LoadAsync(db, studentCode, ct);
            var type = registrationType ?? rules.Classify(course.MaMonHoc, course.MaHocKy);
            if (rules.Block(course, type, selfRegistration: registrationType is not null) is string blocked) return new EnrollmentResult(409, blocked);
            var planned = await db.LopHocPhanDuKiens.AsNoTracking().Include(x => x.ApDung).ThenInclude(x => x.KeHoach)
                .SingleOrDefaultAsync(x => x.MaLopHocPhan == classCode, ct);
            if (planned is not null)
            {
                var plan = planned.ApDung.KeHoach;
                var majorCode = plan.MaNganh; var intakeCode = plan.MaKhoa;
                var majorName = await db.Database.SqlQuery<string>($"SELECT tenNganh AS Value FROM NganhHoc WHERE maNganh={majorCode}").FirstOrDefaultAsync(ct);
                var intakeYear = await db.Database.SqlQuery<int>($"SELECT namBatDau AS Value FROM KhoaTuyenSinh WHERE maKhoa={intakeCode}").FirstOrDefaultAsync(ct);
                if ((!Equal(student.Nganh, majorCode) && !Equal(student.Nganh, majorName)) ||
                    (!Equal(student.KhoaHoc, intakeCode) && !Equal(student.KhoaHoc, intakeYear.ToString())))
                    return new EnrollmentResult(409, "Lớp không thuộc chương trình của ngành/khóa tuyển sinh trong hồ sơ sinh viên.");
            }
            if (await db.DanhSachSinhVienLops.AnyAsync(x => x.MaSinhVien == studentCode && x.LopHocPhan.MaHocKy == course.MaHocKy && x.LopHocPhan.MaMonHoc == course.MaMonHoc, ct))
                return new EnrollmentResult(409, "Bạn đã đăng ký một lớp của môn này trong cùng học kỳ.");
            if (await db.DanhSachSinhVienLops.CountAsync(x => x.MaLopHocPhan == classCode, ct) >= course.SiSoToiDa)
                return new EnrollmentResult(409, "Lớp đã đủ sĩ số.");
            var schedules = await db.LichHocs.AsNoTracking().Where(x => x.MaLopHocPhan == classCode).ToListAsync(ct);
            foreach (var schedule in schedules)
                if (await db.LichHocs.AnyAsync(x => x.LopHocPhan.MaHocKy == course.MaHocKy && x.ThuTrongTuan == schedule.ThuTrongTuan &&
                    x.TietBatDau <= schedule.TietKetThuc && schedule.TietBatDau <= x.TietKetThuc &&
                    x.LopHocPhan.DanhSachSinhVien.Any(s => s.MaSinhVien == studentCode), ct))
                    return new EnrollmentResult(409, "Lịch học trùng lớp bạn đã đăng ký.");
            db.DanhSachSinhVienLops.Add(new DanhSachSinhVienLop { MaSinhVien = studentCode, MaLopHocPhan = classCode, LoaiDangKy = type, ThoiGianDangKy = DateTime.UtcNow, TrangThaiCanhBao = "0" });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return new EnrollmentResult(200, "Đăng ký học phần thành công.");
        });
    private static bool Equal(string? left, string? right) => !string.IsNullOrWhiteSpace(right) && string.Equals(left?.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
