using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

[ApiController, Route("api/v1/dao-tao/dang-ky")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
public sealed class RegistrationSettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet("dot")]
    public async Task<IActionResult> Windows(CancellationToken ct) => Ok(await db.DotDangKyHocPhans.AsNoTracking().OrderByDescending(x => x.MoLucUtc).ToListAsync(ct));

    [HttpPut("dot/{id:guid}")]
    public async Task<IActionResult> Window(Guid id, WindowRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TenDot) || request.TenDot.Length > 200 || request.MoLuc >= request.DongLuc)
            return BadRequest(new { message = "Tên đợt hoặc khoảng thời gian không hợp lệ. Gửi thời gian kèm múi giờ." });
        if (!await db.HocKys.AnyAsync(x => x.MaHocKy == request.MaHocKy, ct)
            || request.KeHoachDaoTaoId is Guid plan && !await db.KeHoachDaoTaos.AnyAsync(x => x.Id == plan, ct)) return BadRequest(new { message = "Học kỳ hoặc chương trình chưa tồn tại." });
        var row = await db.DotDangKyHocPhans.FindAsync([id], ct);
        if (row is null) { row = new() { Id = id }; db.DotDangKyHocPhans.Add(row); }
        row.TenDot = request.TenDot.Trim(); row.MaHocKy = request.MaHocKy; row.KeHoachDaoTaoId = request.KeHoachDaoTaoId;
        row.MoLucUtc = request.MoLuc.UtcDateTime; row.DongLucUtc = request.DongLuc.UtcDateTime; row.ChoPhepDangKy = request.ChoPhepDangKy; row.LaHocKyHe = request.LaHocKyHe;
        await db.SaveChangesAsync(ct); return Ok(new { message = "Đã lưu đợt đăng ký." });
    }

    [HttpPut("hoc-vu/{student}")]
    public async Task<IActionResult> AcademicProfile(string student, AcademicProfileRequest request, CancellationToken ct)
    {
        if (request.ThuTuHocKy is < 1 or > 20 || request.GpaTichLuy is < 0 or > 4 || !await db.SinhViens.AnyAsync(x => x.MaSinhVien == student, ct))
            return BadRequest(new { message = "Sinh viên, tiến độ hoặc GPA hệ 4 không hợp lệ." });
        var row = await db.HoSoHocVus.FindAsync([student], ct);
        if (row is null) { row = new() { MaSinhVien = student }; db.HoSoHocVus.Add(row); }
        row.ThuTuHocKy = request.ThuTuHocKy; row.GpaTichLuy = request.GpaTichLuy; row.LaHocKyDau = request.LaHocKyDau; row.LaHocKyCuoi = request.LaHocKyCuoi;
        await db.SaveChangesAsync(ct); return Ok(new { message = "Đã lưu hồ sơ học vụ." });
    }

    [HttpPut("ket-qua/{student}/{course}/{semester}")]
    public async Task<IActionResult> Grade(string student, string course, string semester, GradeRequest request, CancellationToken ct)
    {
        if (!StudentRegistrationRules.Grades.Contains(request.DiemChu) || !await db.SinhViens.AnyAsync(x => x.MaSinhVien == student, ct)
            || !await db.MonHocs.AnyAsync(x => x.MaMonHoc == course, ct) || !await db.HocKys.AnyAsync(x => x.MaHocKy == semester, ct))
            return BadRequest(new { message = "Kết quả, sinh viên, môn học hoặc học kỳ không hợp lệ." });
        var row = await db.KetQuaHocPhans.FindAsync([student, course, semester], ct);
        if (row is null) { row = new() { MaSinhVien = student, MaMonHoc = course, MaHocKy = semester }; db.KetQuaHocPhans.Add(row); }
        row.DiemChu = request.DiemChu; row.DaCongBo = request.DaCongBo;
        await db.SaveChangesAsync(ct); return Ok(new { message = "Đã lưu kết quả học phần; lịch sử các học kỳ khác được giữ nguyên." });
    }

    [HttpPut("dieu-kien/{plan:guid}/{course}/{required}")]
    public async Task<IActionResult> Requirement(Guid plan, string course, string required, RequirementRequest request, CancellationToken ct)
    {
        if (course == required || request.Loai is not ("tienquyet" or "hoctruoc" or "songhanh")
            || !await db.MonKeHoachDaoTaos.AnyAsync(x => x.KeHoachDaoTaoId == plan && x.MaMonHoc == course, ct)
            || !await db.MonHocs.AnyAsync(x => x.MaMonHoc == required, ct)) return BadRequest(new { message = "Điều kiện môn học không hợp lệ." });
        var row = await db.DieuKienHocPhans.FindAsync([plan, course, required], ct);
        if (row is null) { row = new() { KeHoachDaoTaoId = plan, MaMonHoc = course, MaMonDieuKien = required }; db.DieuKienHocPhans.Add(row); }
        row.Loai = request.Loai; await db.SaveChangesAsync(ct); return Ok(new { message = "Đã lưu điều kiện học phần." });
    }
}

public sealed record WindowRequest(string TenDot, string MaHocKy, Guid? KeHoachDaoTaoId, DateTimeOffset MoLuc, DateTimeOffset DongLuc, bool ChoPhepDangKy, bool LaHocKyHe);
public sealed record AcademicProfileRequest(int ThuTuHocKy, decimal? GpaTichLuy, bool LaHocKyDau, bool LaHocKyCuoi);
public sealed record GradeRequest(string DiemChu, bool DaCongBo);
public sealed record RequirementRequest(string Loai);
