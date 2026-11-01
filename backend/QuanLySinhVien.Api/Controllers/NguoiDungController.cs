using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Users;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class NguoiDungController(AppDbContext db, IAccountProvisioningService accountProvisioning) : ControllerBase
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
<<<<<<< HEAD
    [Authorize]
=======
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
>>>>>>> origin/main
    public async Task<ActionResult<SinhVien>> GetSinhVienById(string maSinhVien, CancellationToken ct)
    {
        var item = await db.SinhViens.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == maSinhVien, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("sinh-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<SinhVien>> CreateSinhVien(CreateSinhVienRequest request, CancellationToken ct)
    {
        var code = request.MaSinhVien.Trim();
        if (await db.SinhViens.AnyAsync(x => x.MaSinhVien == code, ct))
            return Conflict(new { message = "Mã sinh viên đã tồn tại." });
<<<<<<< HEAD
=======
        var email = request.Email.Trim();
        if (await db.SinhViens.AnyAsync(x => x.Email == email, ct)
            || await db.Users.AnyAsync(x => x.Email == email, ct))
            return Conflict(new { message = "Email đã được sử dụng." });
>>>>>>> origin/main

        var item = new SinhVien
        {
            MaSinhVien = code,
            HoTen = request.HoTen.Trim(),
<<<<<<< HEAD
            Email = request.Email.Trim(),
            SoDienThoai = request.SoDienThoai?.Trim(),
            TrangThaiTaiKhoan = true
        };
=======
            Email = email,
            SoDienThoai = request.SoDienThoai?.Trim(),
            TrangThaiTaiKhoan = true
        };
        ApplyStudentProfile(item, request);
>>>>>>> origin/main
        db.SinhViens.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetSinhVienById), new { maSinhVien = item.MaSinhVien }, item);
    }

    [HttpPut("sinh-vien/{maSinhVien}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
<<<<<<< HEAD
    public async Task<IActionResult> UpdateSinhVien(string maSinhVien, SinhVien request, CancellationToken ct)
    {
        var item = await db.SinhViens.FindAsync([maSinhVien], ct); if (item is null) return NotFound();
        item.HoTen = request.HoTen; item.Email = request.Email; item.SoDienThoai = request.SoDienThoai; item.TrangThaiTaiKhoan = request.TrangThaiTaiKhoan;
        await db.SaveChangesAsync(ct); return NoContent();
=======
    public async Task<IActionResult> UpdateSinhVien(string maSinhVien, UpdateSinhVienRequest request, CancellationToken ct)
    {
        var item = await db.SinhViens.FindAsync([maSinhVien], ct);
        if (item is null) return NotFound();

        var email = request.Email.Trim();
        if (await db.Users.AnyAsync(x => (!item.UserId.HasValue || x.Id != item.UserId) && x.Email == email, ct)
            || await db.SinhViens.AnyAsync(x => x.MaSinhVien != maSinhVien && x.Email == email, ct))
            return Conflict(new { message = "Email đã được tài khoản khác sử dụng." });

        item.HoTen = request.HoTen.Trim();
        item.Email = email;
        item.SoDienThoai = request.SoDienThoai?.Trim();
        ApplyStudentProfile(item, request);
        if (item.UserId.HasValue)
        {
            var user = await db.Users.FindAsync([item.UserId.Value], ct);
            if (user is not null)
            {
                user.FullName = item.HoTen;
                user.Email = email;
            }
        }
        await db.SaveChangesAsync(ct);
        return NoContent();
>>>>>>> origin/main
    }

    [HttpGet("giang-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
<<<<<<< HEAD
    public Task<List<GiangVien>> GetGiangVien(CancellationToken ct) => db.GiangViens.AsNoTracking().OrderBy(x => x.MaGiangVien).ToListAsync(ct);
=======
    public Task<List<GiangVien>> GetGiangVien(
        string? search,
        string? khoa,
        string? chuyenNganh,
        CancellationToken ct)
    {
        var query = db.GiangViens.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.MaGiangVien.Contains(keyword) || x.HoTen.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(khoa))
            query = query.Where(x => x.Khoa == khoa.Trim());
        if (!string.IsNullOrWhiteSpace(chuyenNganh))
            query = query.Where(x => x.ChuyenNganh == chuyenNganh.Trim());
        return query.OrderBy(x => x.MaGiangVien).ToListAsync(ct);
    }

    [HttpGet("giang-vien/{maGiangVien}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<GiangVien>> GetGiangVienById(string maGiangVien, CancellationToken ct)
    {
        var item = await db.GiangViens.AsNoTracking().SingleOrDefaultAsync(x => x.MaGiangVien == maGiangVien, ct);
        return item is null ? NotFound() : Ok(item);
    }
>>>>>>> origin/main

    [HttpPost("giang-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<GiangVien>> CreateGiangVien(CreateGiangVienRequest request, CancellationToken ct)
    {
        var code = request.MaGiangVien.Trim();
        if (await db.GiangViens.AnyAsync(x => x.MaGiangVien == code, ct))
            return Conflict(new { message = "Mã giảng viên đã tồn tại." });
<<<<<<< HEAD
=======
        var email = request.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email)
            && (await db.GiangViens.AnyAsync(x => x.Email == email, ct)
                || await db.Users.AnyAsync(x => x.Email == email, ct)))
            return Conflict(new { message = "Email đã được sử dụng." });
>>>>>>> origin/main

        var item = new GiangVien
        {
            MaGiangVien = code,
            HoTen = request.HoTen.Trim(),
            TrangThaiTaiKhoan = true
        };
<<<<<<< HEAD
        db.GiangViens.Add(item);
        await db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, item);
=======
        ApplyLecturerProfile(item, request);
        db.GiangViens.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetGiangVienById), new { maGiangVien = item.MaGiangVien }, item);
    }

    [HttpPut("giang-vien/{maGiangVien}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> UpdateGiangVien(string maGiangVien, UpdateGiangVienRequest request, CancellationToken ct)
    {
        var item = await db.GiangViens.FindAsync([maGiangVien], ct);
        if (item is null) return NotFound();

        var email = request.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email)
            && (await db.GiangViens.AnyAsync(x => x.MaGiangVien != maGiangVien && x.Email == email, ct)
                || await db.Users.AnyAsync(x => (!item.UserId.HasValue || x.Id != item.UserId) && x.Email == email, ct)))
            return Conflict(new { message = "Email đã được tài khoản khác sử dụng." });

        item.HoTen = request.HoTen.Trim();
        ApplyLecturerProfile(item, request);
        if (item.UserId.HasValue)
        {
            var user = await db.Users.FindAsync([item.UserId.Value], ct);
            if (user is not null)
            {
                user.FullName = item.HoTen;
                if (!string.IsNullOrWhiteSpace(email)) user.Email = email;
            }
        }
        await db.SaveChangesAsync(ct);
        return NoContent();
>>>>>>> origin/main
    }

    [HttpPost("sinh-vien/{maSinhVien}/tai-khoan")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> ProvisionSinhVienAccount(
        string maSinhVien,
        ProvisionSinhVienAccountRequest request,
        CancellationToken ct)
    {
        var result = await accountProvisioning.ProvisionSinhVienAsync(maSinhVien, request.InitialPassword, ct);
        return MapProvisioningResult(result, "sinh viên");
    }

    [HttpPost("giang-vien/{maGiangVien}/tai-khoan")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> ProvisionGiangVienAccount(
        string maGiangVien,
        ProvisionGiangVienAccountRequest request,
        CancellationToken ct)
    {
        var result = await accountProvisioning.ProvisionGiangVienAsync(maGiangVien, request.Email, request.InitialPassword, ct);
        return MapProvisioningResult(result, "giảng viên");
    }

    [HttpGet("sinh-vien/{maSinhVien}/ho-so-khuon-mat")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<object>> HoSoKhuonMat(string maSinhVien, CancellationToken ct)
    {
        var profile = await db.HoSoKhuonMats.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == maSinhVien, ct);
        return profile is null ? NotFound() : Ok(new { profile.MaHoSo, profile.MaSinhVien, profile.ThoiGianCapNhat, daDangKy = true });
    }

    private IActionResult MapProvisioningResult(AccountProvisioningResult result, string profileLabel) => result.Status switch
    {
        AccountProvisioningStatus.Created => StatusCode(StatusCodes.Status201Created, result.Account),
        AccountProvisioningStatus.ProfileNotFound => NotFound(new { message = $"Không tìm thấy hồ sơ {profileLabel}. Phải tạo hồ sơ trước khi cấp tài khoản." }),
        AccountProvisioningStatus.ProfileInactive => BadRequest(new { message = $"Hồ sơ {profileLabel} đang bị khóa." }),
        AccountProvisioningStatus.ProfileDataInvalid => BadRequest(new { message = $"Hồ sơ {profileLabel} chưa có đủ dữ liệu để cấp tài khoản." }),
        AccountProvisioningStatus.AlreadyHasAccount => Conflict(new { message = $"Hồ sơ {profileLabel} đã được liên kết với một tài khoản." }),
        AccountProvisioningStatus.UserNameAlreadyExists => Conflict(new { message = "Mã đăng nhập đã tồn tại trong Users." }),
        AccountProvisioningStatus.EmailAlreadyExists => Conflict(new { message = "Email đã được một tài khoản khác sử dụng." }),
        AccountProvisioningStatus.RoleNotConfigured => Problem("Vai trò hệ thống chưa được cấu hình.", statusCode: StatusCodes.Status500InternalServerError),
        _ => Conflict(new { message = "Dữ liệu đã thay đổi hoặc xảy ra xung đột khi cấp tài khoản." })
    };
<<<<<<< HEAD
=======

    private static void ApplyStudentProfile(SinhVien item, PersonalAcademicProfileRequest request)
    {
        item.AnhDaiDien = request.AnhDaiDien?.Trim();
        item.GioiTinh = request.GioiTinh?.Trim();
        item.NgaySinh = request.NgaySinh;
        item.NoiSinh = request.NoiSinh?.Trim();
        item.DanToc = request.DanToc?.Trim();
        item.TonGiao = request.TonGiao?.Trim();
        item.QuocTich = request.QuocTich?.Trim();
        item.SoCCCD = request.SoCCCD?.Trim();
        item.NgayCapCCCD = request.NgayCapCCCD;
        item.NoiCapCCCD = request.NoiCapCCCD?.Trim();
        item.DiaChiLienHe = request.DiaChiLienHe?.Trim();
        item.HoKhauThuongTru = request.HoKhauThuongTru?.Trim();

        if (request is CreateSinhVienRequest create)
        {
            ApplyStudentAcademic(item, create.KhuVuc, create.MaHoSo, create.NgayVaoTruong,
                create.LopHoc, create.CoSo, create.BacDaoTao, create.LoaiHinhDaoTao,
                create.Khoa, create.Nganh, create.ChuyenNganh, create.KhoaHoc,
                create.DoiTuong, create.DienChinhSach, create.NgayVaoDoan, create.NgayVaoDang);
        }
        else if (request is UpdateSinhVienRequest update)
        {
            ApplyStudentAcademic(item, update.KhuVuc, update.MaHoSo, update.NgayVaoTruong,
                update.LopHoc, update.CoSo, update.BacDaoTao, update.LoaiHinhDaoTao,
                update.Khoa, update.Nganh, update.ChuyenNganh, update.KhoaHoc,
                update.DoiTuong, update.DienChinhSach, update.NgayVaoDoan, update.NgayVaoDang);
        }
    }

    private static void ApplyStudentAcademic(
        SinhVien item,
        string? khuVuc,
        string? maHoSo,
        DateTime? ngayVaoTruong,
        string? lopHoc,
        string? coSo,
        string? bacDaoTao,
        string? loaiHinhDaoTao,
        string? khoa,
        string? nganh,
        string? chuyenNganh,
        string? khoaHoc,
        string? doiTuong,
        string? dienChinhSach,
        DateTime? ngayVaoDoan,
        DateTime? ngayVaoDang)
    {
        item.KhuVuc = khuVuc?.Trim();
        item.MaHoSo = maHoSo?.Trim();
        item.NgayVaoTruong = ngayVaoTruong;
        item.LopHoc = lopHoc?.Trim();
        item.CoSo = coSo?.Trim();
        item.BacDaoTao = bacDaoTao?.Trim();
        item.LoaiHinhDaoTao = loaiHinhDaoTao?.Trim();
        item.Khoa = khoa?.Trim();
        item.Nganh = nganh?.Trim();
        item.ChuyenNganh = chuyenNganh?.Trim();
        item.KhoaHoc = khoaHoc?.Trim();
        item.DoiTuong = doiTuong?.Trim();
        item.DienChinhSach = dienChinhSach?.Trim();
        item.NgayVaoDoan = ngayVaoDoan;
        item.NgayVaoDang = ngayVaoDang;
    }

    private static void ApplyLecturerProfile(GiangVien item, PersonalAcademicProfileRequest request)
    {
        item.AnhDaiDien = request.AnhDaiDien?.Trim();
        item.GioiTinh = request.GioiTinh?.Trim();
        item.NgaySinh = request.NgaySinh;
        item.NoiSinh = request.NoiSinh?.Trim();
        item.DanToc = request.DanToc?.Trim();
        item.TonGiao = request.TonGiao?.Trim();
        item.QuocTich = request.QuocTich?.Trim();
        item.SoCCCD = request.SoCCCD?.Trim();
        item.NgayCapCCCD = request.NgayCapCCCD;
        item.NoiCapCCCD = request.NoiCapCCCD?.Trim();
        item.DiaChiLienHe = request.DiaChiLienHe?.Trim();
        item.HoKhauThuongTru = request.HoKhauThuongTru?.Trim();

        if (request is CreateGiangVienRequest create)
        {
            ApplyLecturerWork(item, create.Email, create.SoDienThoai, create.NgayBatDauCongTac,
                create.CoSo, create.Khoa, create.ChuyenNganh, create.TrinhDoHocVan,
                create.ChucDanh, create.NgayVaoDang);
        }
        else if (request is UpdateGiangVienRequest update)
        {
            ApplyLecturerWork(item, update.Email, update.SoDienThoai, update.NgayBatDauCongTac,
                update.CoSo, update.Khoa, update.ChuyenNganh, update.TrinhDoHocVan,
                update.ChucDanh, update.NgayVaoDang);
        }
    }

    private static void ApplyLecturerWork(
        GiangVien item,
        string? email,
        string? phone,
        DateTime? startDate,
        string? campus,
        string? faculty,
        string? specialization,
        string? educationLevel,
        string? title,
        DateTime? partyDate)
    {
        item.Email = email?.Trim();
        item.SoDienThoai = phone?.Trim();
        item.NgayBatDauCongTac = startDate;
        item.CoSo = campus?.Trim();
        item.Khoa = faculty?.Trim();
        item.ChuyenNganh = specialization?.Trim();
        item.TrinhDoHocVan = educationLevel?.Trim();
        item.ChucDanh = title?.Trim();
        item.NgayVaoDang = partyDate;
    }
>>>>>>> origin/main
}
