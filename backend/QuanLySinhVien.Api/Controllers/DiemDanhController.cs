using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Faces;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class DiemDanhController(
    AppDbContext db,
    IFaceRecognitionService faces,
    ILogger<DiemDanhController> logger) : ControllerBase
{
    [HttpPost("buoi-hoc/{maBuoiHoc}/phien-diem-danh")]
    [Authorize(Roles = RoleNames.GiangVien + "," + RoleNames.CanBoQuanLy + "," + RoleNames.Admin)]
    public async Task<ActionResult<object>> MoPhien(string maBuoiHoc, MoPhienRequest request, CancellationToken ct)
    {
        if (!await db.BuoiHocs.AnyAsync(x => x.MaBuoiHoc == maBuoiHoc, ct)) return NotFound();
        if (!await CanManageSessionAsync(maBuoiHoc, ct)) return Forbid();
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
        if (!await CanManageSessionAsync(session.MaBuoiHoc, ct)) return Forbid();
        session.DongLucUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPut("buoi-hoc/{maBuoiHoc}/diem-danh/{maSinhVien}")]
    [Authorize(Roles = RoleNames.GiangVien + "," + RoleNames.CanBoQuanLy + "," + RoleNames.Admin)]
    public async Task<IActionResult> DieuChinh(string maBuoiHoc, string maSinhVien, DieuChinhDiemDanhRequest request, CancellationToken ct)
    {
        if (request.TrangThai is not ("CoMat" or "DiTre" or "CoPhep" or "Vang"))
            return BadRequest(new { message = "Trạng thái điểm danh không hợp lệ." });
        if (!await CanManageSessionAsync(maBuoiHoc, ct)) return Forbid();
        var session = await db.BuoiHocs.AsNoTracking().Where(x => x.MaBuoiHoc == maBuoiHoc)
            .Select(x => new { x.TrangThaiBuoiHoc, x.NgayHocCuThe, x.LichHoc.MaLopHocPhan, x.LichHoc.TietBatDau, x.LichHoc.TietKetThuc })
            .SingleOrDefaultAsync(ct);
        if (session is null) return NotFound();
        if (session.TrangThaiBuoiHoc == "DaChot")
            return Conflict(new { code = "AttendanceFinalized", message = "Buổi học đã chốt điểm danh." });
        // Giảng viên chỉ sửa trong giờ lên lớp; học vụ vẫn sửa được để xử lý sai sót.
        if (!User.IsInRole(RoleNames.Admin) && !User.IsInRole(RoleNames.CanBoQuanLy)
            && !ClassPeriods.DangTrongKhungGio(session.NgayHocCuThe, session.TietBatDau, session.TietKetThuc))
            return Conflict(new { code = "OutsideAttendanceWindow", message = "Chỉ điểm danh được trong giờ lên lớp của buổi hôm nay." });
        var classCode = session.MaLopHocPhan;
        if (!await db.DanhSachSinhVienLops.AnyAsync(
                x => x.MaLopHocPhan == classCode && x.MaSinhVien == maSinhVien, ct))
            return BadRequest(new { message = "Sinh viên không thuộc lớp học phần." });
        var record = await db.ChiTietDiemDanhs.FindAsync([maBuoiHoc, maSinhVien], ct);
        if (record is null) { record = new ChiTietDiemDanh { MaBuoiHoc = maBuoiHoc, MaSinhVien = maSinhVien }; db.ChiTietDiemDanhs.Add(record); }
        record.TrangThai = request.TrangThai;
        record.ThoiGianCheckIn = request.TrangThai is "CoMat" or "DiTre"
            ? request.ThoiGianCheckInUtc ?? DateTime.UtcNow
            : null;
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("buoi-hoc/{maBuoiHoc}/diem-danh")]
    [Authorize(Roles = RoleNames.GiangVien + "," + RoleNames.CanBoQuanLy + "," + RoleNames.Admin)]
    public async Task<ActionResult<object>> KetQua(string maBuoiHoc, CancellationToken ct)
    {
        if (!await db.BuoiHocs.AnyAsync(x => x.MaBuoiHoc == maBuoiHoc, ct)) return NotFound();
        if (!await CanManageSessionAsync(maBuoiHoc, ct)) return Forbid();
        return Ok(await db.ChiTietDiemDanhs.AsNoTracking()
            .Where(x => x.MaBuoiHoc == maBuoiHoc)
            .Select(x => new { x.MaSinhVien, x.SinhVien.HoTen, x.TrangThai, x.ThoiGianCheckIn })
            .ToListAsync(ct));
    }

    [HttpGet("sinh-vien/buoi-hoc-hom-nay")]
    [Authorize(Roles = RoleNames.SinhVien)]
    public async Task<ActionResult<List<object>>> BuoiHocHomNay(CancellationToken ct)
    {
        var maSinhVien = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(maSinhVien)) return Unauthorized();

        var today = DateTime.UtcNow.Date;

        var sessions = await db.BuoiHocs.AsNoTracking()
            .Where(x => x.NgayHocCuThe.Date == today)
            .Join(
                db.LichHocs.AsNoTracking(),
                bh => bh.MaLichHoc,
                lh => lh.MaLichHoc,
                (bh, lh) => new { bh, lh })
            .Join(
                db.DanhSachSinhVienLops.AsNoTracking().Where(x => x.MaSinhVien == maSinhVien),
                x => x.lh.MaLopHocPhan,
                dsl => dsl.MaLopHocPhan,
                (x, dsl) => new { x.bh, x.lh })
            .Join(
                db.LopHocPhans.AsNoTracking(),
                x => x.lh.MaLopHocPhan,
                lp => lp.MaLopHocPhan,
                (x, lp) => new { x.bh, x.lh, lp })
            .Join(
                db.MonHocs.AsNoTracking(),
                x => x.lp.MaMonHoc,
                mh => mh.MaMonHoc,
                (x, mh) => new { x.bh, x.lh, x.lp, mh })
            .Join(
                db.GiangViens.AsNoTracking(),
                x => x.lp.MaGiangVien,
                gv => gv.MaGiangVien,
                (x, gv) => new { x.bh, x.lh, x.lp, x.mh, gv })
            .Join(
                db.PhongHocs.AsNoTracking(),
                x => x.lh.MaPhong,
                phong => phong.MaPhong,
                (x, phong) => new
                {
                    x.bh.MaBuoiHoc,
                    TenMonHoc = x.mh.TenMonHoc,
                    x.lp.MaMonHoc,
                    x.lp.MaLopHocPhan,
                    GiangVien = x.gv.HoTen,
                    ThoiGian = $"Tiết {x.lh.TietBatDau} - {x.lh.TietKetThuc}",
                    Phong = phong.TenPhong,
                    x.bh.TrangThaiBuoiHoc
                })
            .ToListAsync(ct);

        var maBuoiHocList = sessions.Select(s => s.MaBuoiHoc).ToList();
        var daDiemDanhSet = (await db.ChiTietDiemDanhs.AsNoTracking()
            .Where(x => maBuoiHocList.Contains(x.MaBuoiHoc) && x.MaSinhVien == maSinhVien)
            .Select(x => x.MaBuoiHoc)
            .ToListAsync(ct))
            .ToHashSet();

        var response = sessions.Select(s => new
        {
            s.MaBuoiHoc,
            s.TenMonHoc,
            s.MaMonHoc,
            s.MaLopHocPhan,
            s.GiangVien,
            s.ThoiGian,
            s.Phong,
            s.TrangThaiBuoiHoc,
            DaDiemDanh = daDiemDanhSet.Contains(s.MaBuoiHoc)
        }).ToList();

        return Ok(response);
    }

    [HttpPost("buoi-hoc/{maBuoiHoc}/diem-danh-khuon-mat")]
    [Authorize(Roles = RoleNames.SinhVien)]
    [RequestSizeLimit(64 * 1024)]
    public async Task<ActionResult<FaceAttendanceResponse>> DiemDanhKhuonMat(
        string maBuoiHoc,
        FaceDescriptorSamplesRequest request,
        CancellationToken ct)
    {
        var maSinhVien = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(maSinhVien)) return Unauthorized();

        var session = await db.BuoiHocs
            .Where(x => x.MaBuoiHoc == maBuoiHoc)
            .Select(x => new
            {
                x.MaBuoiHoc,
                x.TrangThaiBuoiHoc,
                MaLopHocPhan = x.LichHoc.MaLopHocPhan
            })
            .SingleOrDefaultAsync(ct);
        if (session is null) return NotFound(new { message = "Không tìm thấy buổi học." });
        if (session.TrangThaiBuoiHoc == "DaChot")
            return Conflict(new { code = "AttendanceFinalized", message = "Buổi học đã chốt điểm danh." });

        var isEnrolled = await db.DanhSachSinhVienLops.AnyAsync(
            x => x.MaLopHocPhan == session.MaLopHocPhan && x.MaSinhVien == maSinhVien, ct);
        if (!isEnrolled)
            return StatusCode(403, new { code = "NotEnrolled", message = "Sinh viên chưa đăng ký lớp học phần này." });

        var normalized = faces.NormalizeSamples(request.Descriptors);
        if (!normalized.Succeeded)
            return UnprocessableEntity(new { code = normalized.ErrorCode, message = normalized.ErrorMessage });

        var match = await faces.MatchStudentInClassAsync(session.MaLopHocPhan, normalized.Value!, ct);
        if (!match.Succeeded)
            return UnprocessableEntity(new { code = match.ErrorCode, message = match.ErrorMessage });

        // match.Succeeded đã được kiểm tra ngay trên, nên Value chắc chắn có giá trị.
        if (!string.Equals(match.Value!.MaSinhVien, maSinhVien, StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new { code = "FaceMismatch", message = "Khuôn mặt không khớp với tài khoản đăng nhập." });

        var attendance = await db.ChiTietDiemDanhs.FindAsync([maBuoiHoc, match.Value.MaSinhVien], ct);
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
            "Student face check-in {Result} for student {StudentCode}, session {SessionCode}.",
            alreadyCheckedIn ? "already-recorded" : "recorded",
            match.Value.MaSinhVien,
            maBuoiHoc);

        return Ok(new FaceAttendanceResponse(
            match.Value.MaSinhVien,
            match.Value.HoTen,
            maBuoiHoc,
            attendance.TrangThai,
            checkedInAt,
            alreadyCheckedIn));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private async Task<bool> CanManageSessionAsync(string maBuoiHoc, CancellationToken ct)
    {
        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.CanBoQuanLy)) return true;
        var lecturerCode = User.Identity?.Name;
        return !string.IsNullOrWhiteSpace(lecturerCode)
            && await db.BuoiHocs.AnyAsync(x => x.MaBuoiHoc == maBuoiHoc
                && x.LichHoc.LopHocPhan.MaGiangVien == lecturerCode, ct);
    }
}

public sealed record MoPhienRequest(int SoPhutHieuLuc = 10);
public sealed record DieuChinhDiemDanhRequest(string TrangThai, DateTime? ThoiGianCheckInUtc);
