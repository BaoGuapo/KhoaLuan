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
    [Authorize]
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

        var item = new SinhVien
        {
            MaSinhVien = code,
            HoTen = request.HoTen.Trim(),
            Email = request.Email.Trim(),
            SoDienThoai = request.SoDienThoai?.Trim(),
            TrangThaiTaiKhoan = true
        };
        db.SinhViens.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetSinhVienById), new { maSinhVien = item.MaSinhVien }, item);
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

    [HttpPost("giang-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<GiangVien>> CreateGiangVien(CreateGiangVienRequest request, CancellationToken ct)
    {
        var code = request.MaGiangVien.Trim();
        if (await db.GiangViens.AnyAsync(x => x.MaGiangVien == code, ct))
            return Conflict(new { message = "Mã giảng viên đã tồn tại." });

        var item = new GiangVien
        {
            MaGiangVien = code,
            HoTen = request.HoTen.Trim(),
            TrangThaiTaiKhoan = true
        };
        db.GiangViens.Add(item);
        await db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, item);
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
}
