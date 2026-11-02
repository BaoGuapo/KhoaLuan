using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

/// <summary>
/// Ảnh thẻ chụp lúc nhập học, do học vụ cập nhật từ hồ sơ gốc. Dùng làm ảnh đại diện của
/// sinh viên; ảnh mã hóa trong CSDL nên chỉ trả cho chính sinh viên hoặc học vụ.
/// </summary>
[ApiController, Route("api/v1/anh-the"), Authorize]
public sealed class IdentityPhotoController(AppDbContext db, FaceReviewPhotos photos) : ControllerBase
{
    [HttpGet("cua-toi"), Authorize(Roles = RoleNames.SinhVien)]
    public Task<IActionResult> Mine(CancellationToken ct) => Photo(User.Identity?.Name, ct);

    [HttpGet("sinh-vien/{code}"), Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public Task<IActionResult> Student(string code, CancellationToken ct) => Photo(code, ct);

    private async Task<IActionResult> Photo(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return NotFound();
        var stored = await db.StudentIdentityPhotos.AsNoTracking()
            .Where(x => x.MaSinhVien == code)
            .Select(x => x.ImageProtected)
            .SingleOrDefaultAsync(ct);
        var image = photos.ReadIdentity(stored);
        if (image is null) return NotFound();
        Response.Headers.CacheControl = "private, max-age=300";
        return File(Convert.FromBase64String(image), "image/jpeg");
    }
}
