using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class LichHocController(AppDbContext db) : ControllerBase
{
    [HttpGet("lop-hoc-phan/{maLopHocPhan}/lich-hoc")]
    [Authorize]
    public async Task<ActionResult<object>> GetByLopHocPhan(string maLopHocPhan, CancellationToken ct) => Ok(await db.LichHocs.AsNoTracking().Where(x => x.MaLopHocPhan == maLopHocPhan)
        .Select(x => new { x.MaLichHoc, x.MaLopHocPhan, x.ThuTrongTuan, x.TietBatDau, x.TietKetThuc, x.MaPhong, TenPhong = x.PhongHoc.TenPhong }).ToListAsync(ct));

    [HttpPost("lop-hoc-phan/{maLopHocPhan}/lich-hoc")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<LichHoc>> Create(string maLopHocPhan, LichHoc request, CancellationToken ct)
    {
        if (request.ThuTrongTuan is < 2 or > 8 || request.TietBatDau <= 0 || request.TietKetThuc < request.TietBatDau) return BadRequest(new { message = "Thứ hoặc tiết học không hợp lệ." });
        if (!await db.LopHocPhans.AnyAsync(x => x.MaLopHocPhan == maLopHocPhan, ct) || !await db.PhongHocs.AnyAsync(x => x.MaPhong == request.MaPhong, ct)) return NotFound();
        var conflict = await db.LichHocs.AnyAsync(x => x.MaPhong == request.MaPhong && x.ThuTrongTuan == request.ThuTrongTuan && x.TietBatDau <= request.TietKetThuc && request.TietBatDau <= x.TietKetThuc, ct);
        if (conflict) return Conflict(new { message = "Phòng học đã có lịch trùng." });
        request.MaLopHocPhan = maLopHocPhan; db.LichHocs.Add(request); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetByLopHocPhan), new { maLopHocPhan }, request);
    }

    [HttpGet("lich-hoc/{maLichHoc}/buoi-hoc")]
    [Authorize]
    public Task<List<BuoiHoc>> GetBuoiHoc(string maLichHoc, CancellationToken ct) => db.BuoiHocs.AsNoTracking().Where(x => x.MaLichHoc == maLichHoc).OrderBy(x => x.NgayHocCuThe).ToListAsync(ct);

    [HttpPost("lich-hoc/{maLichHoc}/buoi-hoc")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<BuoiHoc>> CreateBuoiHoc(string maLichHoc, BuoiHoc request, CancellationToken ct)
    {
        if (!await db.LichHocs.AnyAsync(x => x.MaLichHoc == maLichHoc, ct)) return NotFound();
        request.MaLichHoc = maLichHoc; db.BuoiHocs.Add(request); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetBuoiHoc), new { maLichHoc }, request);
    }
}
