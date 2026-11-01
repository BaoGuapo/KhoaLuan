using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Faces;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
[EnableRateLimiting("face-recognition")]
public sealed class FaceRecognitionController(
    AppDbContext db,
    ILogger<FaceRecognitionController> logger) : ControllerBase
{
    [HttpGet("me/ho-so-khuon-mat")]
    [Authorize(Roles = RoleNames.SinhVien)]
    public async Task<ActionResult<FaceProfileMetadataResponse>> GetMyProfile(CancellationToken ct)
    {
        var studentCode = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(studentCode)) return Unauthorized();
        if (!await db.SinhViens.AsNoTracking().AnyAsync(x => x.MaSinhVien == studentCode, ct))
            return NotFound(new { message = "Không tìm thấy hồ sơ sinh viên." });

        var updatedAt = await db.HoSoKhuonMats.AsNoTracking()
            .Where(x => x.MaSinhVien == studentCode)
            .Select(x => (DateTime?)x.ThoiGianCapNhat)
            .SingleOrDefaultAsync(ct);
        return Ok(new FaceProfileMetadataResponse(studentCode, updatedAt.HasValue, updatedAt));
    }

    [HttpPost("me/ho-so-khuon-mat/dang-ky")]
    [RequestSizeLimit(64 * 1024)]
    [Authorize(Roles = RoleNames.SinhVien)]
    public Task<IActionResult> RegisterMyProfile(FaceDescriptorSamplesRequest request, CancellationToken ct) =>
        Task.FromResult<IActionResult>(StatusCode(410, new { code = "LiveEvidenceRequired", message = "Đăng ký bằng camera qua /api/v1/qr-diem-danh/ho-so." }));

    [HttpPut("me/ho-so-khuon-mat")]
    [RequestSizeLimit(64 * 1024)]
    [Authorize(Roles = RoleNames.SinhVien)]
    public Task<IActionResult> ReplaceMyProfile(FaceDescriptorSamplesRequest request, CancellationToken ct) =>
        Task.FromResult<IActionResult>(StatusCode(410, new { code = "LiveEvidenceRequired", message = "Không thể thay hồ sơ bằng vector. Liên hệ giảng viên để đăng ký lại." }));

    [HttpDelete("sinh-vien/{maSinhVien}/ho-so-khuon-mat")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> DeleteProfile(string maSinhVien, CancellationToken ct)
    {
        var profile = await db.HoSoKhuonMats.SingleOrDefaultAsync(x => x.MaSinhVien == maSinhVien, ct);
        if (profile is null) return NotFound(new { message = "Sinh viên chưa đăng ký khuôn mặt." });
        db.HoSoKhuonMats.Remove(profile);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Face profile removed for student {StudentCode} by {Actor}.", maSinhVien, User.Identity?.Name);
        return NoContent();
    }

    [HttpPost("giang-day/buoi-hoc/{maBuoiHoc}/nhan-dien-khuon-mat")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy + "," + RoleNames.GiangVien)]
    public IActionResult RecognizeAttendance(string maBuoiHoc) =>
        StatusCode(410, new { code = "QrAttendanceRequired", message = "Mở phiên QR để sinh viên xác minh trực tiếp qua camera. Không nhận vector điểm danh từ client." });
}
