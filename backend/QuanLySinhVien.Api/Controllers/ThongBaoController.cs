using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
<<<<<<< HEAD
=======
using QuanLySinhVien.Api.Services;
>>>>>>> origin/main

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1/thong-bao")]
<<<<<<< HEAD
public sealed class ThongBaoController(AppDbContext db) : ControllerBase
=======
public sealed class ThongBaoController(AppDbContext db, AttendanceEmailNotifier notifier) : ControllerBase
>>>>>>> origin/main
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
<<<<<<< HEAD
=======
        await notifier.NotifyAnnouncementAsync(request, ct);
>>>>>>> origin/main
        return CreatedAtAction(nameof(GetAll), new { id = request.Id }, request);
    }
}
