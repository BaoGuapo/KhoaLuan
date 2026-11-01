using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class DaoTaoController(AppDbContext db) : ControllerBase
{
    [HttpGet("mon-hoc")]
    [Authorize]
    public Task<List<MonHoc>> GetMonHoc(CancellationToken ct) => db.MonHocs.AsNoTracking().OrderBy(x => x.MaMonHoc).ToListAsync(ct);

    [HttpPost("mon-hoc")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<MonHoc>> CreateMonHoc(MonHoc request, CancellationToken ct)
    {
        if (await db.MonHocs.AnyAsync(x => x.MaMonHoc == request.MaMonHoc, ct)) return Conflict(new { message = "Mã môn học đã tồn tại." });
        db.MonHocs.Add(request); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetMonHoc), new { maMonHoc = request.MaMonHoc }, request);
    }

    [HttpGet("lop-hoc-phan/{maLopHocPhan}")]
    [Authorize]
    public async Task<ActionResult<object>> GetLopHocPhan(string maLopHocPhan, CancellationToken ct)
    {
        var lop = await db.LopHocPhans.AsNoTracking().Include(x => x.MonHoc).Include(x => x.GiangVien).SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);
        return lop is null ? NotFound() : Ok(lop);
    }

    [HttpPost("lop-hoc-phan/{maLopHocPhan}/sinh-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> DangKy(string maLopHocPhan, DangKyRequest request, CancellationToken ct)
    {
        var lop = await db.LopHocPhans.Include(x => x.DanhSachSinhVien).SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);
        if (lop is null) return NotFound();
        if (lop.DanhSachSinhVien.Count >= lop.SiSoToiDa) return Conflict(new { message = "Lớp đã đủ sĩ số." });
        if (await db.DanhSachSinhVienLops.AnyAsync(x => x.MaLopHocPhan == maLopHocPhan && x.MaSinhVien == request.MaSinhVien, ct)) return Conflict(new { message = "Sinh viên đã đăng ký lớp này." });
        if (!await db.SinhViens.AnyAsync(x => x.MaSinhVien == request.MaSinhVien, ct)) return NotFound(new { message = "Không tìm thấy sinh viên." });
        db.DanhSachSinhVienLops.Add(new DanhSachSinhVienLop { MaLopHocPhan = maLopHocPhan, MaSinhVien = request.MaSinhVien, ThoiGianDangKy = DateTime.UtcNow });
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("sinh-vien/{maSinhVien}/lich-hoc")]
    [Authorize]
    public async Task<ActionResult<object>> LichHoc(string maSinhVien, string? maHocKy, CancellationToken ct)
    {
        var query = db.DanhSachSinhVienLops.AsNoTracking().Where(x => x.MaSinhVien == maSinhVien).SelectMany(x => x.LopHocPhan.LichHocs.Select(l => new { x.LopHocPhan.MaLopHocPhan, x.LopHocPhan.MaHocKy, MonHoc = x.LopHocPhan.MonHoc.TenMonHoc, GiangVien = x.LopHocPhan.GiangVien.HoTen, l.ThuTrongTuan, l.TietBatDau, l.TietKetThuc, Phong = l.PhongHoc.TenPhong }));
        if (!string.IsNullOrWhiteSpace(maHocKy)) query = query.Where(x => x.MaHocKy == maHocKy);
        return Ok(await query.OrderBy(x => x.ThuTrongTuan).ThenBy(x => x.TietBatDau).ToListAsync(ct));
    }
}

public sealed record DangKyRequest(string MaSinhVien);
