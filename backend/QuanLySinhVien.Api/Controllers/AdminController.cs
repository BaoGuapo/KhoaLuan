using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { message = "Admin authorization is working." });
}
