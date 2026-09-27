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
    public Task<List<MonHoc>> GetMonHoc(CancellationToken ct) =>
        db.MonHocs.AsNoTracking().OrderBy(x => x.MaMonHoc).ToListAsync(ct);

    [HttpPost("mon-hoc")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<MonHoc>> CreateMonHoc(MonHoc request, CancellationToken ct)
    {
        if (await db.MonHocs.AnyAsync(x => x.MaMonHoc == request.MaMonHoc, ct))
            return Conflict(new { message = "Mã môn học đã tồn tại." });

        db.MonHocs.Add(request);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetMonHoc), new { maMonHoc = request.MaMonHoc }, request);
    }

    [HttpGet("lop-hoc-phan/{maLopHocPhan}")]
    [Authorize]
    public async Task<ActionResult<object>> GetLopHocPhan(string maLopHocPhan, CancellationToken ct)
    {
        var lop = await db.LopHocPhans
            .AsNoTracking()
            .Include(x => x.MonHoc)
            .Include(x => x.GiangVien)
            .SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);

        return lop is null ? NotFound() : Ok(lop);
    }

    [HttpPost("lop-hoc-phan/{maLopHocPhan}/sinh-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> DangKy(string maLopHocPhan, DangKyRequest request, CancellationToken ct)
    {
        var lop = await db.LopHocPhans
            .Include(x => x.DanhSachSinhVien)
            .SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);

        if (lop is null) return NotFound();
        if (lop.DanhSachSinhVien.Count >= lop.SiSoToiDa)
            return Conflict(new { message = "Lớp đã đủ sĩ số." });
        if (await db.DanhSachSinhVienLops.AnyAsync(
                x => x.MaLopHocPhan == maLopHocPhan && x.MaSinhVien == request.MaSinhVien, ct))
            return Conflict(new { message = "Sinh viên đã đăng ký lớp này." });
        if (!await db.SinhViens.AnyAsync(x => x.MaSinhVien == request.MaSinhVien, ct))
            return NotFound(new { message = "Không tìm thấy sinh viên." });

        db.DanhSachSinhVienLops.Add(new DanhSachSinhVienLop
        {
            MaLopHocPhan = maLopHocPhan,
            MaSinhVien = request.MaSinhVien,
            ThoiGianDangKy = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("sinh-vien/{maSinhVien}/lich-hoc")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy + "," + RoleNames.SinhVien)]
    public async Task<ActionResult<object>> LichHoc(string maSinhVien, string? maHocKy, CancellationToken ct)
    {
        var currentCode = User.Identity?.Name;
        if (string.Equals(maSinhVien, "me", StringComparison.OrdinalIgnoreCase))
            maSinhVien = currentCode ?? string.Empty;

        if (User.IsInRole(RoleNames.SinhVien)
            && !string.Equals(maSinhVien, currentCode, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var query = db.DanhSachSinhVienLops
            .AsNoTracking()
            .Where(x => x.MaSinhVien == maSinhVien)
            .SelectMany(x => x.LopHocPhan.LichHocs.Select(l => new
            {
                x.LopHocPhan.MaLopHocPhan,
                x.LopHocPhan.MaHocKy,
                MonHoc = x.LopHocPhan.MonHoc.TenMonHoc,
                GiangVien = x.LopHocPhan.GiangVien.HoTen,
                l.ThuTrongTuan,
                l.TietBatDau,
                l.TietKetThuc,
                Phong = l.PhongHoc.TenPhong
            }));

        if (!string.IsNullOrWhiteSpace(maHocKy))
            query = query.Where(x => x.MaHocKy == maHocKy);

        return Ok(await query
            .OrderBy(x => x.ThuTrongTuan)
            .ThenBy(x => x.TietBatDau)
            .ToListAsync(ct));
    }

    [HttpGet("dang-ky-hoc-phan/danh-sach")]
    [Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> GetDanhSachMonHocDangKy(
        [FromQuery] string loaiDangKy = "moi",
        CancellationToken ct = default)
    {
        if (loaiDangKy is not ("moi" or "lai" or "caithien"))
            return BadRequest(new { message = "Loại đăng ký không hợp lệ." });

        var maSinhVien = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(maSinhVien)
            || !await db.SinhViens.AnyAsync(x => x.MaSinhVien == maSinhVien && x.TrangThaiTaiKhoan, ct))
            return Unauthorized();

        var lopHocPhans = await db.LopHocPhans
            .AsNoTracking()
            .Where(x => x.TrangThai == "0")
            .Select(x => new
            {
                x.MaLopHocPhan,
                x.MaMonHoc,
                x.MonHoc.TenMonHoc,
                x.MonHoc.SoTinChiLyThuyet,
                x.MonHoc.SoTinChiThucHanh,
                x.MaHocKy,
                x.GiangVien.HoTen,
                x.SiSoToiDa,
                SiSoHienTai = x.DanhSachSinhVien.Count,
                DaDangKy = x.DanhSachSinhVien.Any(ds => ds.MaSinhVien == maSinhVien),
                LichHoc = x.LichHocs.Select(l => new
                {
                    l.ThuTrongTuan,
                    l.TietBatDau,
                    l.TietKetThuc,
                    l.PhongHoc.TenPhong
                })
            })
            .OrderBy(x => x.MaMonHoc)
            .ThenBy(x => x.MaLopHocPhan)
            .ToListAsync(ct);

        return Ok(new { loaiDangKy, data = lopHocPhans });
    }

    [HttpPost("dang-ky-hoc-phan/sinh-vien-tu-dang-ky/{maLopHocPhan}")]
    [Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> SinhVienTuDangKy(string maLopHocPhan, CancellationToken ct)
    {
        var maSinhVien = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(maSinhVien)
            || !await db.SinhViens.AnyAsync(x => x.MaSinhVien == maSinhVien && x.TrangThaiTaiKhoan, ct))
            return Unauthorized();

        var lop = await db.LopHocPhans
            .Include(x => x.DanhSachSinhVien)
            .SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);

        if (lop is null)
            return NotFound(new { message = "Không tìm thấy lớp học phần." });
        if (lop.TrangThai != "0")
            return Conflict(new { message = "Lớp học phần hiện không mở đăng ký." });
        if (lop.DanhSachSinhVien.Count >= lop.SiSoToiDa)
            return Conflict(new { message = "Lớp đã đủ sĩ số." });
        if (lop.DanhSachSinhVien.Any(x => x.MaSinhVien == maSinhVien))
            return Conflict(new { message = "Bạn đã đăng ký lớp học phần này." });

        db.DanhSachSinhVienLops.Add(new DanhSachSinhVienLop
        {
            MaLopHocPhan = maLopHocPhan,
            MaSinhVien = maSinhVien,
            ThoiGianDangKy = DateTime.UtcNow,
            TrangThaiCanhBao = "0"
        });
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Đăng ký học phần thành công." });
    }

    [HttpGet("giang-vien/lich-giang-day")]
    [Authorize(Roles = RoleNames.GiangVien)]
    public async Task<IActionResult> GetLichGiangDay(CancellationToken ct)
    {
        var maGiangVien = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(maGiangVien)) return Unauthorized();

        var lichDay = await db.LichHocs
            .AsNoTracking()
            .Where(x => x.LopHocPhan.MaGiangVien == maGiangVien)
            .Select(x => new
            {
                x.MaLopHocPhan,
                x.LopHocPhan.MonHoc.TenMonHoc,
                x.ThuTrongTuan,
                x.TietBatDau,
                x.TietKetThuc,
                x.PhongHoc.TenPhong,
                SiSoHienTai = x.LopHocPhan.DanhSachSinhVien.Count,
                x.LopHocPhan.SiSoToiDa
            })
            .OrderBy(x => x.ThuTrongTuan)
            .ThenBy(x => x.TietBatDau)
            .ToListAsync(ct);

        return Ok(lichDay);
    }

    [HttpGet("giang-vien/lop-hoc-phan/{maLopHocPhan}/chi-tiet")]
    [Authorize(Roles = RoleNames.GiangVien)]
    public async Task<IActionResult> GetChiTietQuanLyLop(string maLopHocPhan, CancellationToken ct)
    {
        var maGiangVien = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(maGiangVien)) return Unauthorized();

        var lop = await db.LopHocPhans
            .AsNoTracking()
            .Where(x => x.MaLopHocPhan == maLopHocPhan && x.MaGiangVien == maGiangVien)
            .Select(x => new
            {
                x.MaLopHocPhan,
                x.MonHoc.TenMonHoc,
                DanhSachSinhVien = x.DanhSachSinhVien.Select(ds => new
                {
                    ds.MaSinhVien,
                    ds.SinhVien.HoTen,
                    ds.SinhVien.Email,
                    ds.TongSoBuoiVang,
                    ds.TrangThaiCanhBao
                })
            })
            .SingleOrDefaultAsync(ct);

        return lop is null
            ? NotFound(new { message = "Lớp không tồn tại hoặc không thuộc giảng viên hiện tại." })
            : Ok(lop);
    }
}

public sealed record DangKyRequest(string MaSinhVien);
