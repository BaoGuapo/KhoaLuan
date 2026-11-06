using Microsoft.AspNetCore.Authorization;
using QuanLySinhVien.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;

namespace QuanLySinhVien.Api.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
[ApiController]
public class PhongHocController : ControllerBase
{
    private readonly AppDbContext _context;
    public PhongHocController(AppDbContext context) { _context = context; }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        var data = await _context.PhongHocs
            .Select(p => new { p.MaPhong, p.LoaiPhong })
            .ToListAsync();
        return Ok(data);
    }
}