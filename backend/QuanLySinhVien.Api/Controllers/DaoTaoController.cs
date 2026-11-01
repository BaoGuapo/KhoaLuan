using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
<<<<<<< HEAD
=======
using QuanLySinhVien.Api.Services;
>>>>>>> origin/main

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
<<<<<<< HEAD
public sealed class DaoTaoController(AppDbContext db) : ControllerBase
{
    [HttpGet("mon-hoc")]
    [Authorize]
    public Task<List<MonHoc>> GetMonHoc(CancellationToken ct) => db.MonHocs.AsNoTracking().OrderBy(x => x.MaMonHoc).ToListAsync(ct);
=======
public sealed class DaoTaoController(AppDbContext db, TrainingEnrollmentService enrollment) : ControllerBase
{
    [HttpGet("mon-hoc")]
    [Authorize]
    public Task<List<MonHoc>> GetMonHoc(CancellationToken ct) =>
        db.MonHocs.AsNoTracking().OrderBy(x => x.MaMonHoc).ToListAsync(ct);
>>>>>>> origin/main

    [HttpPost("mon-hoc")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<MonHoc>> CreateMonHoc(MonHoc request, CancellationToken ct)
    {
<<<<<<< HEAD
        if (await db.MonHocs.AnyAsync(x => x.MaMonHoc == request.MaMonHoc, ct)) return Conflict(new { message = "Mã môn học đã tồn tại." });
        db.MonHocs.Add(request); await db.SaveChangesAsync(ct);
=======
        if (await db.MonHocs.AnyAsync(x => x.MaMonHoc == request.MaMonHoc, ct))
            return Conflict(new { message = "Mã môn học đã tồn tại." });

        db.MonHocs.Add(request);
        await db.SaveChangesAsync(ct);
>>>>>>> origin/main
        return CreatedAtAction(nameof(GetMonHoc), new { maMonHoc = request.MaMonHoc }, request);
    }

    [HttpGet("lop-hoc-phan/{maLopHocPhan}")]
    [Authorize]
    public async Task<ActionResult<object>> GetLopHocPhan(string maLopHocPhan, CancellationToken ct)
    {
<<<<<<< HEAD
        var lop = await db.LopHocPhans.AsNoTracking().Include(x => x.MonHoc).Include(x => x.GiangVien).SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);
=======
        var lop = await db.LopHocPhans
            .AsNoTracking()
            .Include(x => x.MonHoc)
            .Include(x => x.GiangVien)
            .SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);

>>>>>>> origin/main
        return lop is null ? NotFound() : Ok(lop);
    }

    [HttpPost("lop-hoc-phan/{maLopHocPhan}/sinh-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> DangKy(string maLopHocPhan, DangKyRequest request, CancellationToken ct)
    {
<<<<<<< HEAD
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
=======
        var result = await enrollment.EnrollAsync(request.MaSinhVien, maLopHocPhan, ct);
        return StatusCode(result.StatusCode, new { message = result.Message });
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
        CancellationToken ct = default, [FromQuery] string? hocKy = null)
    {
        if (!StudentRegistrationRules.Types.Contains(loaiDangKy))
            return BadRequest(new { message = "Loại đăng ký không hợp lệ." });

        var maSinhVien = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(maSinhVien)
            || !await db.SinhViens.AnyAsync(x => x.MaSinhVien == maSinhVien && x.TrangThaiTaiKhoan, ct))
            return Unauthorized();

        var student = await db.SinhViens.AsNoTracking().SingleAsync(x => x.MaSinhVien == maSinhVien, ct);
        var rules = await StudentRegistrationRules.LoadAsync(db, maSinhVien, ct);
        var selectedSemester = hocKy ?? rules.CurrentSemester;
        var majorValue = student.Nganh?.Trim() ?? "";
        var intakeValue = student.KhoaHoc?.Trim() ?? "";
        var eligibleMajors = await db.Database.SqlQuery<string>($"SELECT maNganh AS Value FROM NganhHoc WHERE maNganh={majorValue} OR tenNganh={majorValue}").ToListAsync(ct);
        var eligibleIntakes = await db.Database.SqlQuery<string>($"SELECT maKhoa AS Value FROM KhoaTuyenSinh WHERE maKhoa={intakeValue} OR CONVERT(nvarchar(10),namBatDau)={intakeValue}").ToListAsync(ct);
        var eligibleApplications = db.ApDungHocKys.Where(x => eligibleMajors.Contains(x.KeHoach.MaNganh) && eligibleIntakes.Contains(x.KeHoach.MaKhoa)).Select(x => x.Id);
        var lopHocPhans = await db.LopHocPhans
            .AsNoTracking()
            .Where(x => x.TrangThai == "0" && x.LichHocs.Any())
            .Where(x => x.MaHocKy == selectedSemester)
            .Where(x => !db.LopHocPhanDuKiens.Any(p => p.MaLopHocPhan == x.MaLopHocPhan)
                || db.LopHocPhanDuKiens.Any(p => p.MaLopHocPhan == x.MaLopHocPhan && eligibleApplications.Contains(p.ApDungHocKyId)))
            .Select(x => new
            {
                x.MaLopHocPhan,
                x.MaMonHoc,
                x.MonHoc.TenMonHoc,
                SoTinChiLyThuyet = (int?)x.MonHoc.SoTinChiLyThuyet ?? 0,
                SoTinChiThucHanh = (int?)x.MonHoc.SoTinChiThucHanh ?? 0,
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

        var data = lopHocPhans.Where(x => rules.Classify(x.MaMonHoc, x.MaHocKy) == loaiDangKy).Select(x =>
        {
            var course = new LopHocPhan { MaHocKy = x.MaHocKy, MaMonHoc = x.MaMonHoc,
                MonHoc = new MonHoc { SoTinChiLyThuyet = x.SoTinChiLyThuyet, SoTinChiThucHanh = x.SoTinChiThucHanh } };
            var reason = rules.Block(course, loaiDangKy);
            return new { x.MaLopHocPhan, x.MaMonHoc, x.TenMonHoc, x.SoTinChiLyThuyet, x.SoTinChiThucHanh, x.MaHocKy,
                x.HoTen, x.SiSoToiDa, x.SiSoHienTai, x.DaDangKy, x.LichHoc, LoaiDangKy = loaiDangKy, DiemDaCo = rules.PreviousGrade(x.MaMonHoc, x.MaHocKy), LyDoKhongDuDieuKien = reason };
        }).ToArray();
        return Ok(new { loaiDangKy, data, hocKyHienTai = rules.CurrentSemester, hocKyDangXem = selectedSemester,
            dotDangKy = rules.Windows.Where(x => x.MaHocKy == selectedSemester && rules.IsOpen(x)).Select(x => new { x.TenDot, x.MoLucUtc, x.DongLucUtc }),
            dangMoDangKy = rules.Windows.Any(x => x.MaHocKy == selectedSemester && rules.IsOpen(x)),
            tinChiToiThieu = rules.MinCredits(selectedSemester ?? ""), tinChiToiDa = rules.MaxCredits(selectedSemester ?? ""),
            gpaTichLuy = rules.Profile?.GpaTichLuy });
    }

    [HttpPost("dang-ky-hoc-phan/sinh-vien-tu-dang-ky/{maLopHocPhan}")]
    [Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> SinhVienTuDangKy(string maLopHocPhan, CancellationToken ct, [FromQuery] string loaiDangKy = "moi")
    {
        var maSinhVien = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(maSinhVien)
            || !await db.SinhViens.AnyAsync(x => x.MaSinhVien == maSinhVien && x.TrangThaiTaiKhoan, ct))
            return Unauthorized();

        var result = await enrollment.EnrollAsync(maSinhVien, maLopHocPhan, ct, loaiDangKy);
        return StatusCode(result.StatusCode, new { message = result.Message });
    }

    [Authorize(Roles = RoleNames.SinhVien)]
    [HttpGet("dang-ky-hoc-phan/da-dang-ky")]
    public async Task<IActionResult> MyEnrollments(CancellationToken ct)
    {
        var code = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(code)) return Unauthorized();
        return Ok(await db.DanhSachSinhVienLops.AsNoTracking().Where(x => x.MaSinhVien == code)
            .OrderBy(x => x.LopHocPhan.MaHocKy).ThenBy(x => x.LopHocPhan.MaMonHoc)
            .Select(x => new
            {
                x.MaLopHocPhan, x.LopHocPhan.MaMonHoc, x.LopHocPhan.MonHoc.TenMonHoc, x.LoaiDangKy,
                SoTinChiLyThuyet = (int?)x.LopHocPhan.MonHoc.SoTinChiLyThuyet ?? 0, SoTinChiThucHanh = (int?)x.LopHocPhan.MonHoc.SoTinChiThucHanh ?? 0,
                x.LopHocPhan.MaHocKy, HoTen = x.LopHocPhan.GiangVien.HoTen, ThoiGianDangKy = (DateTime?)x.ThoiGianDangKy,
                LichHoc = x.LopHocPhan.LichHocs.Select(l => new
                    { l.ThuTrongTuan, l.TietBatDau, l.TietKetThuc, l.PhongHoc.TenPhong })
            }).ToListAsync(ct));
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
>>>>>>> origin/main
    }
}

public sealed record DangKyRequest(string MaSinhVien);
