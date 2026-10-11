using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuanLySinhVien.Api.Contracts.Auth;
using QuanLySinhVien.Api.Contracts.Faces;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IFaceRecognitionService faces) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return response is null ? Unauthorized(new { message = "Invalid credentials." }) : Ok(response);
    }

    [Authorize(Roles = RoleNames.SinhVien)]
    [EnableRateLimiting("face-login")]
    [RequestSizeLimit(64 * 1024)]
    [HttpPost("verify-face")]
    public async Task<IActionResult> VerifyFace(FaceDescriptorSamplesRequest request, CancellationToken cancellationToken)
    {
        var studentCode = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(studentCode)) return Unauthorized();

        var normalized = faces.NormalizeSamples(request.Descriptors);
        if (!normalized.Succeeded)
            return UnprocessableEntity(new { code = normalized.ErrorCode, message = normalized.ErrorMessage });

        return await faces.VerifyStudentAsync(studentCode, normalized.Value!, cancellationToken)
            ? NoContent()
            : Unauthorized(new { code = "FaceNotMatched", message = "Khuôn mặt không khớp hồ sơ đã đăng ký." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await authService.ChangePasswordAsync(userId, request, cancellationToken)
            ? NoContent()
            : BadRequest(new { message = "Current password is invalid." });
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        return response is null ? Unauthorized(new { message = "Refresh token is invalid or expired." }) : Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await authService.LogoutAsync(userId, request.RefreshToken, cancellationToken);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
