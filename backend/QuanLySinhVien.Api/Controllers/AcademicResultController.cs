using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Academic;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

/// <summary>Bảng điểm gọn của sinh viên: điểm chữ đã công bố theo học kỳ, GPA và chuyên cần từng môn.</summary>
[ApiController, Route("api/v1/me/ket-qua-hoc-tap"), Authorize(Roles = RoleNames.SinhVien)]
public sealed class AcademicResultController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AcademicResultResponse>> Get(CancellationToken ct)
    {
        var student = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(student)) return Unauthorized();

        var enrollments = await db.DanhSachSinhVienLops.AsNoTracking()
            .Where(x => x.MaSinhVien == student && x.LopHocPhan.MaHocKy != null)
            .Select(x => new
            {
                x.MaLopHocPhan,
                x.LopHocPhan.MaMonHoc,
                MaHocKy = x.LopHocPhan.MaHocKy!,
                x.TrangThaiCanhBao,
                CoMat = db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == student
                    && d.BuoiHoc.LichHoc.MaLopHocPhan == x.MaLopHocPhan && d.TrangThai == "CoMat"),
                DiTre = db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == student
                    && d.BuoiHoc.LichHoc.MaLopHocPhan == x.MaLopHocPhan && d.TrangThai == "DiTre"),
                CoPhep = db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == student
                    && d.BuoiHoc.LichHoc.MaLopHocPhan == x.MaLopHocPhan && d.TrangThai == "CoPhep"),
                Vang = db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == student
                    && d.BuoiHoc.LichHoc.MaLopHocPhan == x.MaLopHocPhan && d.TrangThai == "Vang"),
                TongSoBuoi = db.BuoiHocs.Count(b => b.LichHoc.MaLopHocPhan == x.MaLopHocPhan)
            })
            .ToListAsync(ct);

        // Điểm chưa công bố không được lộ ra, kể cả dưới dạng "chưa có điểm".
        var grades = await db.KetQuaHocPhans.AsNoTracking()
            .Where(x => x.MaSinhVien == student && x.DaCongBo)
            .Select(x => new { x.MaMonHoc, x.MaHocKy, x.DiemChu })
            .ToListAsync(ct);

        var courseCodes = enrollments.Select(x => x.MaMonHoc).Concat(grades.Select(x => x.MaMonHoc)).Distinct().ToList();
        var subjects = await db.MonHocs.AsNoTracking()
            .Where(x => courseCodes.Contains(x.MaMonHoc))
            .Select(x => new { x.MaMonHoc, x.TenMonHoc, x.SoTinChiLyThuyet, x.SoTinChiThucHanh, x.SoTinChi })
            .ToDictionaryAsync(x => x.MaMonHoc, ct);
        var semesters = await db.HocKys.AsNoTracking()
            .Select(x => new TranscriptSemester(x.MaHocKy, x.TenHocKy, x.NgayBatDau))
            .ToListAsync(ct);

        var gradeByKey = grades
            .GroupBy(x => (x.MaMonHoc, x.MaHocKy))
            .ToDictionary(x => x.Key, x => x.First().DiemChu);
        var rows = new List<TranscriptCourse>();
        foreach (var e in enrollments)
        {
            gradeByKey.Remove((e.MaMonHoc, e.MaHocKy), out var diem);
            rows.Add(Row(e.MaMonHoc, e.MaHocKy, e.MaLopHocPhan, diem,
                new AttendanceTallyResponse(e.CoMat, e.DiTre, e.CoPhep, e.Vang, e.TongSoBuoi, e.TrangThaiCanhBao)));
        }
        // Kết quả nhập tay từ học vụ (ví dụ môn học trước khi có hệ thống) không gắn với lớp học phần nào.
        foreach (var ((course, semester), diem) in gradeByKey)
            rows.Add(Row(course, semester, null, diem, null));

        return Ok(AcademicTranscript.Build(semesters, rows));

        TranscriptCourse Row(string course, string semester, string? classCode, string? diem, AttendanceTallyResponse? tally)
        {
            subjects.TryGetValue(course, out var subject);
            var credits = subject is null ? 0 : subject.SoTinChiLyThuyet + subject.SoTinChiThucHanh;
            if (credits == 0 && subject is not null) credits = subject.SoTinChi;
            return new TranscriptCourse(course, subject?.TenMonHoc ?? course, semester, classCode, credits, diem, tally);
        }
    }
}
