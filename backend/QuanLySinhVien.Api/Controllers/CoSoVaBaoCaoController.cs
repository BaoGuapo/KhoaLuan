using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class CoSoVaBaoCaoController(AppDbContext db) : ControllerBase
{
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
        var rows = await db.DanhSachSinhVienLops.AsNoTracking().Where(x => x.MaLopHocPhan == maLopHocPhan)
            .Select(x => new { x.MaSinhVien, x.SinhVien.HoTen, CoMat = db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == x.MaSinhVien && d.BuoiHoc.LichHoc.MaLopHocPhan == maLopHocPhan && d.TrangThai == "CoMat"), Vang = db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == x.MaSinhVien && d.BuoiHoc.LichHoc.MaLopHocPhan == maLopHocPhan && d.TrangThai == "Vang") }).ToListAsync(ct);
        return Ok(rows);
    }
}
