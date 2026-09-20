using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class DiemDanhController(AppDbContext db) : ControllerBase
{
    [HttpPost("buoi-hoc/{maBuoiHoc}/phien-diem-danh")]
    [Authorize(Roles = RoleNames.GiangVien + "," + RoleNames.CanBoQuanLy + "," + RoleNames.Admin)]
    public async Task<ActionResult<object>> MoPhien(string maBuoiHoc, MoPhienRequest request, CancellationToken ct)
    {
        if (!await db.BuoiHocs.AnyAsync(x => x.MaBuoiHoc == maBuoiHoc, ct)) return NotFound();
        var qrToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); var now = DateTime.UtcNow;
        var session = new PhienDiemDanh { MaBuoiHoc = maBuoiHoc, QrTokenHash = Hash(qrToken), MoLucUtc = now, HetHanLucUtc = now.AddMinutes(Math.Clamp(request.SoPhutHieuLuc, 1, 30)) };
        db.PhienDiemDanhs.Add(session); await db.SaveChangesAsync(ct);
        return Ok(new { maPhien = session.Id, qrToken, hetHanLucUtc = session.HetHanLucUtc });
    }

    [HttpPost("phien-diem-danh/{maPhien:guid}/dong")]
    [Authorize(Roles = RoleNames.GiangVien + "," + RoleNames.CanBoQuanLy + "," + RoleNames.Admin)]
    public async Task<IActionResult> DongPhien(Guid maPhien, CancellationToken ct)
    {
        var session = await db.PhienDiemDanhs.FindAsync([maPhien], ct); if (session is null) return NotFound();
        session.DongLucUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPut("buoi-hoc/{maBuoiHoc}/diem-danh/{maSinhVien}")]
    [Authorize(Roles = RoleNames.GiangVien + "," + RoleNames.CanBoQuanLy + "," + RoleNames.Admin)]
    public async Task<IActionResult> DieuChinh(string maBuoiHoc, string maSinhVien, DieuChinhDiemDanhRequest request, CancellationToken ct)
    {
        var record = await db.ChiTietDiemDanhs.FindAsync([maBuoiHoc, maSinhVien], ct);
        if (record is null) { record = new ChiTietDiemDanh { MaBuoiHoc = maBuoiHoc, MaSinhVien = maSinhVien }; db.ChiTietDiemDanhs.Add(record); }
        record.TrangThai = request.TrangThai; record.ThoiGianCheckIn = request.ThoiGianCheckInUtc ?? DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("buoi-hoc/{maBuoiHoc}/diem-danh")]
    [Authorize(Roles = RoleNames.GiangVien + "," + RoleNames.CanBoQuanLy + "," + RoleNames.Admin)]
    public async Task<ActionResult<object>> KetQua(string maBuoiHoc, CancellationToken ct) => Ok(await db.ChiTietDiemDanhs.AsNoTracking().Where(x => x.MaBuoiHoc == maBuoiHoc).Select(x => new { x.MaSinhVien, x.SinhVien.HoTen, x.TrangThai, x.ThoiGianCheckIn }).ToListAsync(ct));

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed record MoPhienRequest(int SoPhutHieuLuc = 10);
public sealed record DieuChinhDiemDanhRequest(string TrangThai, DateTime? ThoiGianCheckInUtc);
