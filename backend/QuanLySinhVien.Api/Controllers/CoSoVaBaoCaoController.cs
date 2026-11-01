using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
<<<<<<< HEAD
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
=======
using QuanLySinhVien.Api.Contracts.Reporting;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;
>>>>>>> origin/main

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
<<<<<<< HEAD
public sealed class CoSoVaBaoCaoController(AppDbContext db) : ControllerBase
{
=======
public sealed class CoSoVaBaoCaoController(AppDbContext db, AttendanceReportService reports) : ControllerBase
{
    private const string ReportViewers = RoleNames.Admin + "," + RoleNames.CanBoQuanLy;

    [HttpGet("bao-cao/thong-ke-diem-danh")]
    [Authorize(Roles = ReportViewers)]
    public async Task<ActionResult<AttendanceReportResponse>> ThongKeDiemDanh(
        string maHocKy,
        CancellationToken ct)
    {
        var report = await reports.BuildAsync(maHocKy, ct);
        return report is null ? NotFound(new { message = "Không tìm thấy học kỳ." }) : Ok(report);
    }

    [HttpGet("bao-cao/thong-ke-diem-danh/xuat-excel")]
    [Authorize(Roles = ReportViewers)]
    public async Task<IActionResult> XuatThongKeDiemDanh(string maHocKy, CancellationToken ct)
    {
        var report = await reports.BuildAsync(maHocKy, ct);
        if (report is null) return NotFound(new { message = "Không tìm thấy học kỳ." });
        var fileName = $"bao-cao-diem-danh-{Sanitize(maHocKy)}.xlsx";
        return File(AttendanceReportExcel.Write(report),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>Mã học kỳ do người dùng nhập nên phải lọc trước khi ghép vào tên tệp tải về.</summary>
    private static string Sanitize(string value)
    {
        var safe = new string(value.Where(x => char.IsLetterOrDigit(x) || x is '-' or '_').ToArray());
        return safe.Length == 0 ? "hoc-ky" : safe;
    }

>>>>>>> origin/main
    [HttpGet("phong-hoc")]
    [Authorize]
    public Task<List<PhongHoc>> GetPhongHoc(CancellationToken ct) => db.PhongHocs.AsNoTracking().OrderBy(x => x.MaPhong).ToListAsync(ct);

    [HttpPost("phong-hoc")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<PhongHoc>> CreatePhongHoc(PhongHoc request, CancellationToken ct)
    {
        if (await db.PhongHocs.AnyAsync(x => x.MaPhong == request.MaPhong, ct)) return Conflict(new { message = "Mã phòng đã tồn tại." });
        db.PhongHocs.Add(request); await db.SaveChangesAsync(ct); return Ok(request);
    }

    [HttpGet("hoc-ky")]
    [Authorize]
    public Task<List<HocKyThucTe>> GetHocKy(CancellationToken ct) => db.HocKys.AsNoTracking().OrderByDescending(x => x.NgayBatDau).ToListAsync(ct);

    [HttpGet("bao-cao/chuyen-can/lop-hoc-phan/{maLopHocPhan}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy + "," + RoleNames.GiangVien)]
    public async Task<ActionResult<object>> ChuyenCan(string maLopHocPhan, CancellationToken ct)
    {
<<<<<<< HEAD
=======
        if (User.IsInRole(RoleNames.GiangVien)
            && !await db.LopHocPhans.AnyAsync(x => x.MaLopHocPhan == maLopHocPhan
                && x.MaGiangVien == User.Identity!.Name, ct))
            return Forbid();
>>>>>>> origin/main
        var rows = await db.DanhSachSinhVienLops.AsNoTracking().Where(x => x.MaLopHocPhan == maLopHocPhan)
            .Select(x => new { x.MaSinhVien, x.SinhVien.HoTen, CoMat = db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == x.MaSinhVien && d.BuoiHoc.LichHoc.MaLopHocPhan == maLopHocPhan && d.TrangThai == "CoMat"), Vang = db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == x.MaSinhVien && d.BuoiHoc.LichHoc.MaLopHocPhan == maLopHocPhan && d.TrangThai == "Vang") }).ToListAsync(ct);
        return Ok(rows);
    }
}
