using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class NguoiDungController(AppDbContext db) : ControllerBase
{
    [HttpGet("sinh-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public Task<List<SinhVien>> GetSinhVien(string? search, CancellationToken ct)
    {
        var query = db.SinhViens.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.MaSinhVien.Contains(search) || x.HoTen.Contains(search));
        return query.OrderBy(x => x.MaSinhVien).ToListAsync(ct);
    }

    [HttpGet("sinh-vien/{maSinhVien}")]
    [Authorize]
    public async Task<ActionResult<SinhVien>> GetSinhVienById(string maSinhVien, CancellationToken ct)
    {
        var item = await db.SinhViens.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == maSinhVien, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("sinh-vien/{maSinhVien}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> UpdateSinhVien(string maSinhVien, SinhVien request, CancellationToken ct)
    {
        var item = await db.SinhViens.FindAsync([maSinhVien], ct); if (item is null) return NotFound();
        item.HoTen = request.HoTen; item.Email = request.Email; item.SoDienThoai = request.SoDienThoai; item.TrangThaiTaiKhoan = request.TrangThaiTaiKhoan;
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("giang-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public Task<List<GiangVien>> GetGiangVien(CancellationToken ct) => db.GiangViens.AsNoTracking().OrderBy(x => x.MaGiangVien).ToListAsync(ct);

    [HttpGet("sinh-vien/{maSinhVien}/ho-so-khuon-mat")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<object>> HoSoKhuonMat(string maSinhVien, CancellationToken ct)
    {
        var profile = await db.HoSoKhuonMats.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == maSinhVien, ct);
        return profile is null ? NotFound() : Ok(new { profile.MaHoSo, profile.MaSinhVien, profile.ThoiGianCapNhat, daDangKy = true });
    }
}
