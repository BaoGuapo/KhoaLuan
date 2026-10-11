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
    IFaceRecognitionService faces,
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
        SaveMyProfileAsync(request, replaceExisting: false, ct);

    [HttpPut("me/ho-so-khuon-mat")]
    [RequestSizeLimit(64 * 1024)]
    [Authorize(Roles = RoleNames.SinhVien)]
    public Task<IActionResult> ReplaceMyProfile(FaceDescriptorSamplesRequest request, CancellationToken ct) =>
        SaveMyProfileAsync(request, replaceExisting: true, ct);

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
    [RequestSizeLimit(64 * 1024)]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy + "," + RoleNames.GiangVien)]
    public async Task<ActionResult<FaceAttendanceResponse>> RecognizeAttendance(
        string maBuoiHoc,
        FaceDescriptorSamplesRequest request,
        CancellationToken ct)
    {
        var session = await db.BuoiHocs
            .Where(x => x.MaBuoiHoc == maBuoiHoc)
            .Select(x => new
            {
                x.MaBuoiHoc,
                x.TrangThaiBuoiHoc,
                x.LichHoc.MaLopHocPhan,
                x.LichHoc.LopHocPhan.MaGiangVien
            })
            .SingleOrDefaultAsync(ct);
        if (session is null) return NotFound(new { message = "Không tìm thấy buổi học." });
        if (session.TrangThaiBuoiHoc == "DaChot")
            return Conflict(new { code = "AttendanceFinalized", message = "Buổi học đã chốt điểm danh." });
        if (User.IsInRole(RoleNames.GiangVien)
            && !string.Equals(session.MaGiangVien, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var normalized = faces.NormalizeSamples(request.Descriptors);
        if (!normalized.Succeeded)
            return UnprocessableEntity(new { code = normalized.ErrorCode, message = normalized.ErrorMessage });

        var match = await faces.MatchStudentInClassAsync(session.MaLopHocPhan, normalized.Value!, ct);
        if (!match.Succeeded)
            return UnprocessableEntity(new { code = match.ErrorCode, message = match.ErrorMessage });

        var attendance = await db.ChiTietDiemDanhs.FindAsync([maBuoiHoc, match.Value!.MaSinhVien], ct);
        var alreadyCheckedIn = attendance?.TrangThai is "CoMat" or "DiTre";
        if (attendance is null)
        {
            attendance = new ChiTietDiemDanh
            {
                MaBuoiHoc = maBuoiHoc,
                MaSinhVien = match.Value.MaSinhVien
            };
            db.ChiTietDiemDanhs.Add(attendance);
        }

        if (!alreadyCheckedIn)
        {
            attendance.TrangThai = "CoMat";
            attendance.ThoiGianCheckIn = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        var checkedInAt = attendance.ThoiGianCheckIn ?? DateTime.UtcNow;
        logger.LogInformation(
            "Face attendance {Result} for student {StudentCode}, session {SessionCode}, actor {Actor}.",
            alreadyCheckedIn ? "already-recorded" : "recorded",
            match.Value.MaSinhVien,
            maBuoiHoc,
            User.Identity?.Name);

        return Ok(new FaceAttendanceResponse(
            match.Value.MaSinhVien,
            match.Value.HoTen,
            maBuoiHoc,
            attendance.TrangThai,
            checkedInAt,
            alreadyCheckedIn));
    }

    private async Task<IActionResult> SaveMyProfileAsync(
        FaceDescriptorSamplesRequest request,
        bool replaceExisting,
        CancellationToken ct)
    {
        var studentCode = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(studentCode)) return Unauthorized();
        if (!await db.SinhViens.AnyAsync(x => x.MaSinhVien == studentCode, ct))
            return NotFound(new { message = "Không tìm thấy hồ sơ sinh viên." });

        var normalized = faces.NormalizeSamples(request.Descriptors);
        if (!normalized.Succeeded)
            return UnprocessableEntity(new { code = normalized.ErrorCode, message = normalized.ErrorMessage });

        var profile = await db.HoSoKhuonMats.SingleOrDefaultAsync(x => x.MaSinhVien == studentCode, ct);
        if (profile is not null && !replaceExisting)
            return Conflict(new { code = "ProfileAlreadyExists", message = "Sinh viên đã có hồ sơ khuôn mặt." });
        if (profile is null && replaceExisting)
            return NotFound(new { code = "ProfileNotFound", message = "Sinh viên chưa có hồ sơ khuôn mặt để cập nhật." });

        var created = profile is null;
        if (profile is null)
        {
            profile = new HoSoKhuonMat
            {
                MaHoSo = $"FACE-{Guid.NewGuid():N}",
                MaSinhVien = studentCode
            };
            db.HoSoKhuonMats.Add(profile);
        }
        profile.VectorJson = faces.ProtectDescriptor(normalized.Value!);
        profile.ThoiGianCapNhat = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Face profile {Action} for student {StudentCode}.", replaceExisting ? "replaced" : "registered", studentCode);
        return created ? StatusCode(StatusCodes.Status201Created) : NoContent();
    }
}
