using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Teaching;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1/giang-day")]
public sealed class TeachingManagementController(AppDbContext db) : ControllerBase
{
    private static readonly HashSet<string> AttendanceStatuses =
        ["CoMat", "DiTre", "CoPhep", "Vang"];

    [HttpGet("giang-vien/{maGiangVien}/phan-cong")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<LecturerAssignmentWorkspaceResponse>> GetAssignmentWorkspace(
        string maGiangVien,
        CancellationToken ct)
    {
        var lecturer = await db.GiangViens.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaGiangVien == maGiangVien, ct);
        if (lecturer is null) return NotFound(new { message = "Không tìm thấy giảng viên." });

        var courses = await GetEligibleCoursesAsync(lecturer.ChuyenNganh, ct);
        var semesters = await db.HocKys.AsNoTracking()
            .OrderByDescending(x => x.NgayBatDau)
            .Select(x => new AcademicOptionResponse(x.MaHocKy, x.TenHocKy))
            .ToListAsync(ct);
        var rooms = await db.PhongHocs.AsNoTracking()
            .OrderBy(x => x.MaPhong)
            .Select(x => new AcademicOptionResponse(x.MaPhong, x.TenPhong))
            .ToListAsync(ct);
        var assignments = await GetAssignmentsAsync(maGiangVien, ct);

        return Ok(new LecturerAssignmentWorkspaceResponse(
            lecturer.MaGiangVien,
            lecturer.HoTen,
            lecturer.Khoa,
            lecturer.ChuyenNganh,
            courses,
            semesters,
            rooms,
            assignments));
    }

    [HttpPost("giang-vien/{maGiangVien}/phan-cong")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<TeachingAssignmentResponse>> CreateAssignment(
        string maGiangVien,
        CreateTeachingAssignmentRequest request,
        CancellationToken ct)
    {
        var lecturer = await db.GiangViens.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaGiangVien == maGiangVien, ct);
        if (lecturer is null) return NotFound(new { message = "Không tìm thấy giảng viên." });
        if (!lecturer.TrangThaiTaiKhoan)
            return BadRequest(new { message = "Giảng viên đang bị khóa." });
        if (string.IsNullOrWhiteSpace(lecturer.ChuyenNganh))
            return BadRequest(new { message = "Cần cập nhật chuyên ngành cho giảng viên trước khi phân công." });
        if (request.TietKetThuc < request.TietBatDau)
            return BadRequest(new { message = "Tiết kết thúc phải lớn hơn hoặc bằng tiết bắt đầu." });

        var eligibleCourses = await GetEligibleCoursesAsync(lecturer.ChuyenNganh, ct);
        if (!eligibleCourses.Any(x => x.MaMonHoc == request.MaMonHoc))
            return BadRequest(new { message = "Môn học không thuộc chương trình của chuyên ngành giảng viên." });

        var semester = await db.HocKys.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaHocKy == request.MaHocKy, ct);
        if (semester is null) return NotFound(new { message = "Không tìm thấy học kỳ." });
        if (semester.NgayBatDau is null || semester.NgayKetThuc is null || semester.NgayKetThuc < semester.NgayBatDau || (semester.NgayKetThuc.Value - semester.NgayBatDau.Value).TotalDays > 366)
            return BadRequest(new { message = "Khoảng ngày học kỳ không hợp lệ." });
        if (!await db.PhongHocs.AnyAsync(x => x.MaPhong == request.MaPhong, ct))
            return NotFound(new { message = "Không tìm thấy phòng học." });

        var classCode = request.MaLopHocPhan.Trim();
        if (await db.LopHocPhans.AnyAsync(x => x.MaLopHocPhan == classCode, ct))
            return Conflict(new { message = "Mã lớp học phần đã tồn tại." });

        var roomConflict = await db.LichHocs.AnyAsync(x =>
            x.LopHocPhan.MaHocKy == request.MaHocKy
            && x.MaPhong == request.MaPhong
            && x.ThuTrongTuan == request.ThuTrongTuan
            && x.TietBatDau <= request.TietKetThuc
            && request.TietBatDau <= x.TietKetThuc, ct);
        if (roomConflict) return Conflict(new { message = "Phòng học đã có lịch trùng trong học kỳ này." });

        var lecturerConflict = await db.LichHocs.AnyAsync(x =>
            x.LopHocPhan.MaHocKy == request.MaHocKy
            && x.LopHocPhan.MaGiangVien == maGiangVien
            && x.ThuTrongTuan == request.ThuTrongTuan
            && x.TietBatDau <= request.TietKetThuc
            && request.TietBatDau <= x.TietKetThuc, ct);
        if (lecturerConflict) return Conflict(new { message = "Giảng viên đã có lịch dạy trùng thời gian." });

        var scheduleCode = $"LH-{classCode}";
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.LopHocPhans.Add(new LopHocPhan
        {
            MaLopHocPhan = classCode,
            MaMonHoc = request.MaMonHoc,
            MaHocKy = request.MaHocKy,
            MaGiangVien = maGiangVien,
            SiSoToiDa = request.SiSoToiDa,
            TrangThai = "0"
        });
        db.LichHocs.Add(new LichHoc
        {
            MaLichHoc = scheduleCode,
            MaLopHocPhan = classCode,
            MaPhong = request.MaPhong,
            ThuTrongTuan = request.ThuTrongTuan,
            TietBatDau = request.TietBatDau,
            TietKetThuc = request.TietKetThuc
        });

        foreach (var date in DatesForWeekday(semester.NgayBatDau.Value, semester.NgayKetThuc.Value, request.ThuTrongTuan))
        {
            db.BuoiHocs.Add(new BuoiHoc
            {
                MaBuoiHoc = $"BH-{classCode}-{date:yyyyMMdd}",
                MaLichHoc = scheduleCode,
                NgayHocCuThe = date,
                TrangThaiBuoiHoc = "ChuaDienRa"
            });
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var created = (await GetAssignmentsAsync(maGiangVien, ct))
            .Single(x => x.MaLopHocPhan == classCode);
        return CreatedAtAction(nameof(GetClassWorkspace), new { maLopHocPhan = classCode }, created);
    }

    [HttpGet("cua-toi")]
    [Authorize(Roles = RoleNames.GiangVien)]
    public async Task<ActionResult<IReadOnlyCollection<TeachingAssignmentResponse>>> GetMyAssignments(CancellationToken ct)
    {
        var maGiangVien = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(maGiangVien)) return Unauthorized();
        return Ok(await GetAssignmentsAsync(maGiangVien, ct));
    }

    [HttpGet("lop/{maLopHocPhan}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy + "," + RoleNames.GiangVien)]
    public async Task<ActionResult<LecturerClassWorkspaceResponse>> GetClassWorkspace(
        string maLopHocPhan,
        string? maBuoiHoc,
        CancellationToken ct)
    {
        if (!await CanAccessClassAsync(maLopHocPhan, ct)) return Forbid();
        var assignment = await GetAssignmentAsync(maLopHocPhan, ct);
        if (assignment is null) return NotFound(new { message = "Không tìm thấy lớp học phần." });

        var sessions = await db.BuoiHocs.AsNoTracking()
            .Where(x => x.LichHoc.MaLopHocPhan == maLopHocPhan)
            .OrderBy(x => x.NgayHocCuThe)
            .Select(x => new AttendanceSessionResponse(
                x.MaBuoiHoc,
                x.NgayHocCuThe,
                x.TrangThaiBuoiHoc,
                x.TrangThaiBuoiHoc == "DaChot"))
            .ToListAsync(ct);

        var selectedSession = sessions.FirstOrDefault(x => x.MaBuoiHoc == maBuoiHoc)
            ?? sessions.Where(x => x.NgayHocCuThe.Date <= DateTime.Today)
                .OrderByDescending(x => x.NgayHocCuThe)
                .FirstOrDefault()
            ?? sessions.FirstOrDefault();
        var selectedCode = selectedSession?.MaBuoiHoc;
        var totalSessions = sessions.Count;

        var students = await db.DanhSachSinhVienLops.AsNoTracking()
            .Where(x => x.MaLopHocPhan == maLopHocPhan)
            .OrderBy(x => x.MaSinhVien)
            .Select(x => new StudentAttendanceResponse(
                x.MaSinhVien,
                x.SinhVien.HoTen,
                x.SinhVien.Email,
                x.SinhVien.SoDienThoai,
                selectedCode == null
                    ? "ChuaDiemDanh"
                    : db.ChiTietDiemDanhs
                        .Where(d => d.MaBuoiHoc == selectedCode && d.MaSinhVien == x.MaSinhVien)
                        .Select(d => d.TrangThai)
                        .FirstOrDefault() ?? "ChuaDiemDanh",
                selectedCode == null
                    ? null
                    : db.ChiTietDiemDanhs
                        .Where(d => d.MaBuoiHoc == selectedCode && d.MaSinhVien == x.MaSinhVien)
                        .Select(d => d.ThoiGianCheckIn)
                        .FirstOrDefault(),
                db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == x.MaSinhVien
                    && d.BuoiHoc.LichHoc.MaLopHocPhan == maLopHocPhan && d.TrangThai == "CoMat"),
                db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == x.MaSinhVien
                    && d.BuoiHoc.LichHoc.MaLopHocPhan == maLopHocPhan && d.TrangThai == "DiTre"),
                db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == x.MaSinhVien
                    && d.BuoiHoc.LichHoc.MaLopHocPhan == maLopHocPhan && d.TrangThai == "CoPhep"),
                db.ChiTietDiemDanhs.Count(d => d.MaSinhVien == x.MaSinhVien
                    && d.BuoiHoc.LichHoc.MaLopHocPhan == maLopHocPhan && d.TrangThai == "Vang"),
                totalSessions,
                x.TrangThaiCanhBao))
            .ToListAsync(ct);

        return Ok(new LecturerClassWorkspaceResponse(assignment, sessions, selectedCode, students));
    }

    [HttpPut("buoi-hoc/{maBuoiHoc}/sinh-vien/{maSinhVien}/diem-danh")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy + "," + RoleNames.GiangVien)]
    public async Task<IActionResult> UpdateAttendance(
        string maBuoiHoc,
        string maSinhVien,
        UpdateAttendanceRequest request,
        CancellationToken ct)
    {
        if (!AttendanceStatuses.Contains(request.TrangThai))
            return BadRequest(new { message = "Trạng thái điểm danh không hợp lệ." });
        var session = await db.BuoiHocs.AsNoTracking()
            .Where(x => x.MaBuoiHoc == maBuoiHoc)
            .Select(x => new { x.MaBuoiHoc, x.TrangThaiBuoiHoc, x.LichHoc.MaLopHocPhan })
            .SingleOrDefaultAsync(ct);
        if (session is null) return NotFound(new { message = "Không tìm thấy buổi học." });
        if (!await CanAccessClassAsync(session.MaLopHocPhan, ct)) return Forbid();
        if (session.TrangThaiBuoiHoc == "DaChot")
            return Conflict(new { message = "Buổi học đã chốt điểm danh." });
        if (!await db.DanhSachSinhVienLops.AnyAsync(
                x => x.MaLopHocPhan == session.MaLopHocPhan && x.MaSinhVien == maSinhVien, ct))
            return BadRequest(new { message = "Sinh viên không thuộc lớp học phần này." });

        var attendance = await db.ChiTietDiemDanhs.FindAsync([maBuoiHoc, maSinhVien], ct);
        if (attendance is null)
        {
            attendance = new ChiTietDiemDanh { MaBuoiHoc = maBuoiHoc, MaSinhVien = maSinhVien };
            db.ChiTietDiemDanhs.Add(attendance);
        }
        attendance.TrangThai = request.TrangThai;
        attendance.ThoiGianCheckIn = request.TrangThai is "CoMat" or "DiTre" ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("buoi-hoc/{maBuoiHoc}/chot-diem-danh")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy + "," + RoleNames.GiangVien)]
    public async Task<IActionResult> FinalizeAttendance(string maBuoiHoc, CancellationToken ct)
    {
        var session = await db.BuoiHocs
            .Include(x => x.LichHoc)
            .SingleOrDefaultAsync(x => x.MaBuoiHoc == maBuoiHoc, ct);
        if (session is null) return NotFound(new { message = "Không tìm thấy buổi học." });
        if (!await CanAccessClassAsync(session.LichHoc.MaLopHocPhan, ct)) return Forbid();
        if (session.TrangThaiBuoiHoc == "DaChot") return NoContent();

        var enrollments = await db.DanhSachSinhVienLops
            .Where(x => x.MaLopHocPhan == session.LichHoc.MaLopHocPhan)
            .ToListAsync(ct);
        var existingCodes = (await db.ChiTietDiemDanhs
            .Where(x => x.MaBuoiHoc == maBuoiHoc)
            .Select(x => x.MaSinhVien)
            .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var enrollment in enrollments.Where(x => !existingCodes.Contains(x.MaSinhVien)))
        {
            db.ChiTietDiemDanhs.Add(new ChiTietDiemDanh
            {
                MaBuoiHoc = maBuoiHoc,
                MaSinhVien = enrollment.MaSinhVien,
                TrangThai = "Vang"
            });
        }

        session.TrangThaiBuoiHoc = "DaChot";
        session.ThoiGianDiemDanh = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        foreach (var enrollment in enrollments)
        {
            enrollment.TongSoBuoiVang = await db.ChiTietDiemDanhs.CountAsync(x =>
                x.MaSinhVien == enrollment.MaSinhVien
                && x.BuoiHoc.LichHoc.MaLopHocPhan == enrollment.MaLopHocPhan
                && x.TrangThai == "Vang", ct);
            enrollment.TrangThaiCanhBao = enrollment.TongSoBuoiVang switch
            {
                >= 4 => "CamThi",
                >= 2 => "CanhBao",
                _ => "BinhThuong"
            };
        }
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<List<EligibleCourseResponse>> GetEligibleCoursesAsync(
        string? specialization,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(specialization)) return [];
        var value = specialization.Trim();
        var majorCodes = await db.Database.SqlQuery<string>($"SELECT maNganh AS Value FROM NganhHoc WHERE maNganh={value} OR tenNganh={value}").ToListAsync(ct);
        var planCourses = await db.MonKeHoachDaoTaos.AsNoTracking()
            .Where(x => majorCodes.Contains(x.KeHoach.MaNganh))
            .Select(x => new EligibleCourseResponse(x.MaMonHoc, x.MonHoc.TenMonHoc,
                x.MonHoc.SoTinChiLyThuyet, x.MonHoc.SoTinChiThucHanh, x.KeHoach.MaNganh, x.KeHoach.MaNganh))
            .Distinct().ToListAsync(ct);
        var rows = await db.Database.SqlQuery<EligibleCourseRow>(
            $"""
            SELECT DISTINCT
                m.maMonHoc AS MaMonHoc,
                m.tenMonHoc AS TenMonHoc,
                m.soTinChiLyThuyet AS SoTinChiLyThuyet,
                m.soTinChiThucHanh AS SoTinChiThucHanh,
                n.maNganh AS MaNganh,
                n.tenNganh AS TenNganh
            FROM ChuongTrinhDaoTao c
            INNER JOIN MonHoc m ON m.maMonHoc = c.maMonHoc
            INNER JOIN NganhHoc n ON n.maNganh = c.maNganh
            WHERE n.maNganh = {value} OR n.tenNganh = {value}
            """)
            .ToListAsync(ct);

        return rows
            .OrderBy(x => x.MaMonHoc)
            .Select(x => new EligibleCourseResponse(
                x.MaMonHoc,
                x.TenMonHoc,
                x.SoTinChiLyThuyet,
                x.SoTinChiThucHanh,
                x.MaNganh,
                x.TenNganh))
            .Concat(planCourses).DistinctBy(x => x.MaMonHoc).ToList();
    }

    private async Task<List<TeachingAssignmentResponse>> GetAssignmentsAsync(
        string maGiangVien,
        CancellationToken ct) =>
        await db.LichHocs.AsNoTracking()
            .Where(x => x.LopHocPhan.MaGiangVien == maGiangVien)
            .OrderByDescending(x => db.HocKys
                .Where(h => h.MaHocKy == x.LopHocPhan.MaHocKy)
                .Select(h => h.NgayBatDau)
                .FirstOrDefault())
            .ThenBy(x => x.ThuTrongTuan)
            .ThenBy(x => x.TietBatDau)
            .Select(x => new TeachingAssignmentResponse(
                x.LopHocPhan.MaLopHocPhan,
                x.LopHocPhan.MaMonHoc,
                x.LopHocPhan.MonHoc.TenMonHoc,
                x.LopHocPhan.MaHocKy,
                db.HocKys.Where(h => h.MaHocKy == x.LopHocPhan.MaHocKy)
                    .Select(h => h.TenHocKy).FirstOrDefault() ?? x.LopHocPhan.MaHocKy,
                x.LopHocPhan.MaGiangVien,
                x.LopHocPhan.GiangVien.HoTen,
                x.LopHocPhan.DanhSachSinhVien.Count,
                x.LopHocPhan.SiSoToiDa,
                x.LopHocPhan.TrangThai,
                x.MaLichHoc,
                x.ThuTrongTuan,
                x.TietBatDau,
                x.TietKetThuc,
                x.MaPhong,
                x.PhongHoc.TenPhong))
            .ToListAsync(ct);

    private async Task<TeachingAssignmentResponse?> GetAssignmentAsync(
        string maLopHocPhan,
        CancellationToken ct) =>
        await db.LichHocs.AsNoTracking()
            .Where(x => x.MaLopHocPhan == maLopHocPhan)
            .Select(x => new TeachingAssignmentResponse(
                x.LopHocPhan.MaLopHocPhan,
                x.LopHocPhan.MaMonHoc,
                x.LopHocPhan.MonHoc.TenMonHoc,
                x.LopHocPhan.MaHocKy,
                db.HocKys.Where(h => h.MaHocKy == x.LopHocPhan.MaHocKy)
                    .Select(h => h.TenHocKy).FirstOrDefault() ?? x.LopHocPhan.MaHocKy,
                x.LopHocPhan.MaGiangVien,
                x.LopHocPhan.GiangVien.HoTen,
                x.LopHocPhan.DanhSachSinhVien.Count,
                x.LopHocPhan.SiSoToiDa,
                x.LopHocPhan.TrangThai,
                x.MaLichHoc,
                x.ThuTrongTuan,
                x.TietBatDau,
                x.TietKetThuc,
                x.MaPhong,
                x.PhongHoc.TenPhong))
            .FirstOrDefaultAsync(ct);

    private async Task<bool> CanAccessClassAsync(string maLopHocPhan, CancellationToken ct)
    {
        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.CanBoQuanLy)) return true;
        var lecturerCode = User.Identity?.Name;
        return !string.IsNullOrWhiteSpace(lecturerCode)
            && await db.LopHocPhans.AnyAsync(
                x => x.MaLopHocPhan == maLopHocPhan && x.MaGiangVien == lecturerCode, ct);
    }

    private static IEnumerable<DateTime> DatesForWeekday(DateTime start, DateTime end, int weekday)
    {
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var currentWeekday = date.DayOfWeek == DayOfWeek.Sunday
                ? 8
                : (int)date.DayOfWeek + 1;
            if (currentWeekday == weekday) yield return date;
        }
    }
}

public sealed class EligibleCourseRow
{
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int SoTinChiLyThuyet { get; set; }
    public int SoTinChiThucHanh { get; set; }
    public string MaNganh { get; set; } = string.Empty;
    public string TenNganh { get; set; } = string.Empty;
}
