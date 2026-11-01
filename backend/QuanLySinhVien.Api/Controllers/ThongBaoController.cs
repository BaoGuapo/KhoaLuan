using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1/thong-bao")]
public sealed class ThongBaoController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public Task<List<ThongBao>> GetAll(CancellationToken ct) => db.ThongBaos.AsNoTracking().OrderByDescending(x => x.TaoLucUtc).ToListAsync(ct);

    [HttpPost]
    [Authorize(Roles = RoleNames.GiangVien + "," + RoleNames.CanBoQuanLy + "," + RoleNames.Admin)]
    public async Task<ActionResult<ThongBao>> Create(ThongBao request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TieuDe) || string.IsNullOrWhiteSpace(request.NoiDung)) return BadRequest(new { message = "Tiêu đề và nội dung là bắt buộc." });
        request.Id = Guid.NewGuid(); request.TaoLucUtc = DateTime.UtcNow;
        db.ThongBaos.Add(request); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), new { id = request.Id }, request);
    }
}
