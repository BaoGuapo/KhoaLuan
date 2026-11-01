using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Api.Contracts.Users;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1/nguoi-dung")]
public sealed class UserManagementController(UserManagementService users) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<PagedResponse<UserSummaryResponse>>> GetUsers(
        string? search,
        string? role,
        bool? isActive,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await users.GetUsersAsync(
            search,
            role,
            isActive,
            page,
            pageSize,
            User.IsInRole(RoleNames.Admin),
            ct);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDetailResponse>> GetMe(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await users.GetMyProfileAsync(userId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(UpdateMyProfileRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var error = await users.UpdateMyProfileAsync(userId, request, ct);
        return error is null ? NoContent() : Conflict(new { message = error });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<UserDetailResponse>> GetUser(Guid id, CancellationToken ct)
    {
        if (!await CanManageAsync(id, ct)) return Forbid();
        var result = await users.GetUserAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> CreateStaff(CreateStaffAccountRequest request, CancellationToken ct)
    {
        var (user, error) = await users.CreateStaffAsync(request, ct);
        return user is not null
            ? CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id, user.UserName })
            : Conflict(new { message = error });
    }

    [HttpPut("{id:guid}/trang-thai")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> SetStatus(Guid id, UpdateAccountStatusRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var actorId)) return Unauthorized();
        if (!await CanManageAsync(id, ct)) return Forbid();
        var error = await users.SetStatusAsync(id, request.IsActive, actorId, ct);
        return error is null ? NoContent() : BadRequest(new { message = error });
    }

    [HttpPost("{id:guid}/dat-lai-mat-khau")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request, CancellationToken ct)
    {
        if (!await CanManageAsync(id, ct)) return Forbid();
        var error = await users.ResetPasswordAsync(id, request.TemporaryPassword, ct);
        return error is null ? NoContent() : BadRequest(new { message = error });
    }

    [HttpPut("{id:guid}/vai-tro")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ChangeRole(Guid id, UpdateUserRoleRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var actorId)) return Unauthorized();
        var error = await users.ChangeRoleAsync(id, request.Role.Trim(), actorId, ct);
        return error is null ? NoContent() : BadRequest(new { message = error });
    }

    private async Task<bool> CanManageAsync(Guid targetId, CancellationToken ct) =>
        await users.CanManageTargetAsync(targetId, User.IsInRole(RoleNames.Admin), ct);

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
