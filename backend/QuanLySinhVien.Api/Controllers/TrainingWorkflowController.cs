using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Training;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

[ApiController, Route("api/v1/dao-tao")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
public sealed class TrainingWorkflowController(AppDbContext db) : ControllerBase
{
    [HttpGet("chuong-trinh/mau-excel")]
    public IActionResult ExcelTemplate() => File(CurriculumExcel.Template(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ChuongTrinhDaoTao-mau.xlsx");

    [HttpPost("chuong-trinh/xem-truoc-excel"), RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> PreviewExcel(IFormFile file, CancellationToken ct)
    {
        if (file.Length is < 1 or > 2 * 1024 * 1024 || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Chọn file .xlsx tối đa 2 MB." });
        try
        {
            using var stream = file.OpenReadStream();
            var preview = CurriculumExcel.Read(stream);
            var codes = preview.MonHocs.Select(x => x.MaMonHoc).ToArray();
            var known = await db.MonHocs.Where(x => codes.Contains(x.MaMonHoc)).Select(x => x.MaMonHoc).ToListAsync(ct);
            var errors = preview.Errors.Concat(codes.Except(known, StringComparer.OrdinalIgnoreCase).Select(x => $"Mã môn {x} chưa có trong danh mục.")).ToArray();
            return Ok(new { preview.MonHocs, errors, canSave = errors.Length == 0 });
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException or ArgumentException)
        { return BadRequest(new { message = "File không đúng mẫu XLSX hoặc chứa dữ liệu không được hỗ trợ." }); }
    }
    [HttpGet("danh-muc")]
    public async Task<IActionResult> Catalog(CancellationToken ct)
    {
        var majors = await db.Database.SqlQueryRaw<TrainingOption>(
            "SELECT CAST(maNganh AS nvarchar(50)) AS Code, tenNganh AS Name FROM NganhHoc ORDER BY maNganh").ToListAsync(ct);
        var intakes = await db.Database.SqlQueryRaw<TrainingOption>(
            "SELECT CAST(maKhoa AS nvarchar(50)) AS Code, CONCAT(maKhoa, N' (', namBatDau, N'–', namKetThuc, N')') AS Name FROM KhoaTuyenSinh ORDER BY namBatDau DESC").ToListAsync(ct);
        return Ok(new
        {
            nganhHocs = majors, khoaTuyenSinhs = intakes,
            monHocs = await db.MonHocs.AsNoTracking().OrderBy(x => x.MaMonHoc).ToListAsync(ct),
            hocKys = await db.HocKys.AsNoTracking().OrderByDescending(x => x.NgayBatDau).ToListAsync(ct),
            giangViens = await db.GiangViens.AsNoTracking().Where(x => x.TrangThaiTaiKhoan)
                .Select(x => new { x.MaGiangVien, x.HoTen, x.Khoa, x.ChuyenNganh }).ToListAsync(ct),
            phongHocs = await db.PhongHocs.AsNoTracking().OrderBy(x => x.MaPhong).ToListAsync(ct)
        });
    }

    [HttpPost("nganh-hoc")]
    public async Task<IActionResult> CreateMajor(CreateMajorRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.MaNganh) || string.IsNullOrWhiteSpace(request.TenNganh))
            return BadRequest(new { message = "Nhập mã và tên ngành." });
        var code = request.MaNganh.Trim(); var name = request.TenNganh.Trim();
        try
        {
            var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO NganhHoc(maNganh,tenNganh)
                SELECT {code},{name} WHERE NOT EXISTS(SELECT 1 FROM NganhHoc WITH (UPDLOCK,HOLDLOCK) WHERE maNganh={code})
                """, ct);
            return changed == 0 ? Conflict(new { message = "Mã ngành đã tồn tại." }) : Ok(new { message = "Đã tạo ngành." });
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
        { return Conflict(new { message = "Mã ngành đã tồn tại." }); }
    }

    [HttpPost("khoa-tuyen-sinh")]
    public async Task<IActionResult> CreateIntake(CreateIntakeRequest request, CancellationToken ct)
    {
        if (request.NamKetThuc < request.NamBatDau) return BadRequest(new { message = "Năm kết thúc phải sau năm bắt đầu." });
        var code = request.MaKhoa.Trim();
        try
        {
            var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO KhoaTuyenSinh(maKhoa,namBatDau,namKetThuc)
                SELECT {code},{request.NamBatDau},{request.NamKetThuc}
                WHERE NOT EXISTS(SELECT 1 FROM KhoaTuyenSinh WITH (UPDLOCK,HOLDLOCK) WHERE maKhoa={code})
                """, ct);
            return changed == 0 ? Conflict(new { message = "Khóa tuyển sinh đã tồn tại." }) : Ok(new { message = "Đã tạo khóa tuyển sinh." });
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
        { return Conflict(new { message = "Khóa tuyển sinh đã tồn tại." }); }
    }

    [HttpGet("chuong-trinh")]
    public async Task<IActionResult> Curricula(CancellationToken ct) => Ok(await db.KeHoachDaoTaos.AsNoTracking()
        .OrderBy(x => x.MaNganh).ThenBy(x => x.MaKhoa).Select(x => new
        {
            x.Id, x.MaNganh, x.MaKhoa, x.TenKhoaDaoTao, x.TenChuongTrinh,
            monHocs = x.MonHocs.OrderBy(m => m.ThuTuHocKy).ThenBy(m => m.MaMonHoc).Select(m => new
            { m.MaMonHoc, m.MonHoc.TenMonHoc, m.ThuTuHocKy, m.BatBuoc })
        }).ToListAsync(ct));

    [HttpPost("hoc-ky")]
    public async Task<IActionResult> CreateSemester(CreateSemesterRequest request, CancellationToken ct)
    {
        if (request.NgayBatDau.Year is < 2000 or > 2100 || request.NgayKetThuc < request.NgayBatDau || (request.NgayKetThuc - request.NgayBatDau).TotalDays > 366)
            return BadRequest(new { message = "Khoảng ngày học kỳ phải hợp lệ và tối đa 366 ngày." });
        if (await db.HocKys.AnyAsync(x => x.MaHocKy == request.MaHocKy, ct)) return Conflict(new { message = "Mã học kỳ đã tồn tại." });
        db.HocKys.Add(new HocKyThucTe { MaHocKy = request.MaHocKy.Trim(), TenHocKy = request.TenHocKy.Trim(), LoaiHocKy = "0",
            NgayBatDau = request.NgayBatDau.Date, NgayKetThuc = request.NgayKetThuc.Date });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { return Conflict(new { message = "Mã học kỳ đã tồn tại." }); }
        return Ok(new { message = "Đã tạo học kỳ thực tế." });
    }

    [HttpPost("chuong-trinh")]
    public async Task<IActionResult> CreateCurriculum(CreateCurriculumRequest request, CancellationToken ct)
    {
        var error = TrainingRules.ValidateCourses(request.MonHocs);
        if (error is not null) return BadRequest(new { message = error });
        var major = request.MaNganh.Trim(); var intake = request.MaKhoa.Trim();
        if (!await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM NganhHoc WHERE maNganh={major}").AnyAsync(x => x > 0, ct) ||
            !await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM KhoaTuyenSinh WHERE maKhoa={intake}").AnyAsync(x => x > 0, ct))
            return BadRequest(new { message = "Chọn ngành và khóa tuyển sinh đã tồn tại." });
        var codes = request.MonHocs.Select(x => x.MaMonHoc.Trim()).ToArray();
        var existingCodes = await db.MonHocs.Where(x => codes.Contains(x.MaMonHoc)).Select(x => x.MaMonHoc).ToListAsync(ct);
        if (codes.Except(existingCodes, StringComparer.OrdinalIgnoreCase).Any())
            return BadRequest(new { message = "Có mã môn chưa tồn tại trong danh mục môn học." });
        if (await db.KeHoachDaoTaos.AnyAsync(x => x.MaNganh == major && x.MaKhoa == intake, ct))
            return Conflict(new { message = "Ngành và khóa này đã có chương trình. Không tạo bản trùng." });
        var plan = new KeHoachDaoTao
        {
            MaNganh = major, MaKhoa = intake, TenKhoaDaoTao = request.TenKhoaDaoTao.Trim(), TenChuongTrinh = request.TenChuongTrinh.Trim(),
            MonHocs = request.MonHocs.Select(x => new MonKeHoachDaoTao
            { MaMonHoc = x.MaMonHoc.Trim(), ThuTuHocKy = x.ThuTuHocKy, BatBuoc = x.BatBuoc }).ToList()
        };
        try
        {
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                db.KeHoachDaoTaos.Add(plan);
                await db.SaveChangesAsync(ct);
                var index = 0;
                foreach (var course in request.MonHocs)
                {
                    var rowCode = $"{plan.Id:N}-{++index:000}";
                    var courseCode = course.MaMonHoc.Trim();
                    // Đồng bộ bảng gốc để các chức năng nhóm đã làm tiếp tục đọc CTĐT.
                    await db.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO ChuongTrinhDaoTao(maChuongTrinh,maNganh,maKhoa,maMonHoc,thuTuHocKy)
                        SELECT {rowCode},{major},{intake},{courseCode},{course.ThuTuHocKy}
                        WHERE NOT EXISTS (SELECT 1 FROM ChuongTrinhDaoTao WITH (UPDLOCK,HOLDLOCK)
                          WHERE maNganh={major} AND maKhoa={intake} AND maMonHoc={courseCode} AND thuTuHocKy={course.ThuTuHocKy})
                        """, ct);
                }
                await transaction.CommitAsync(ct);
            });
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { return Conflict(new { message = "Chương trình đã được tạo bởi phiên khác." }); }
        return Ok(new { plan.Id, message = "Đã lưu chương trình đào tạo." });
    }

    [HttpGet("ap-dung-hoc-ky")]
    public async Task<IActionResult> Applications(CancellationToken ct) => Ok(await db.ApDungHocKys.AsNoTracking()
        .Select(x => new { x.Id, x.KeHoachDaoTaoId, x.MaHocKy, x.ThuTuHocKy, x.KeHoach.TenChuongTrinh, x.KeHoach.MaNganh, x.KeHoach.MaKhoa,
            monHocs = x.KeHoach.MonHocs.Where(m => m.ThuTuHocKy == x.ThuTuHocKy).Select(m => new { m.MaMonHoc, m.MonHoc.TenMonHoc, m.BatBuoc }),
            lopHocPhans = x.LopHocPhans.OrderBy(l => l.MaLopHocPhan).Select(l => new { l.MaLopHocPhan, l.MaMonHoc, l.SiSoToiDa, l.DaPhanCong,
                maGiangVien = db.LopHocPhans.Where(p => p.MaLopHocPhan == l.MaLopHocPhan).Select(p => p.MaGiangVien).FirstOrDefault(),
                trangThai = db.LopHocPhans.Where(p => p.MaLopHocPhan == l.MaLopHocPhan).Select(p => p.TrangThai).FirstOrDefault() })
        }).ToListAsync(ct));

    [HttpPost("ap-dung-hoc-ky")]
    public async Task<IActionResult> Apply(ApplySemesterRequest request, CancellationToken ct)
    {
        if (!await db.MonKeHoachDaoTaos.AnyAsync(x => x.KeHoachDaoTaoId == request.KeHoachDaoTaoId && x.ThuTuHocKy == request.ThuTuHocKy, ct))
            return BadRequest(new { message = "Chương trình không có môn ở học kỳ dự kiến này." });
        var semester = await db.HocKys.FindAsync([request.MaHocKy], ct);
        if (semester is null || semester.NgayBatDau is null || semester.NgayKetThuc is null || semester.NgayKetThuc < semester.NgayBatDau || (semester.NgayKetThuc.Value - semester.NgayBatDau.Value).TotalDays > 366)
            return BadRequest(new { message = "Học kỳ thực tế chưa tồn tại hoặc khoảng ngày không hợp lệ." });
        if (await db.ApDungHocKys.AnyAsync(x => x.KeHoachDaoTaoId == request.KeHoachDaoTaoId && x.MaHocKy == request.MaHocKy, ct))
            return Conflict(new { message = "Chương trình đã được áp dụng cho học kỳ này." });
        var applied = new ApDungHocKy { KeHoachDaoTaoId = request.KeHoachDaoTaoId, MaHocKy = request.MaHocKy, ThuTuHocKy = request.ThuTuHocKy };
        db.ApDungHocKys.Add(applied);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { return Conflict(new { message = "Học kỳ đã được áp dụng bởi phiên khác." }); }
        return Ok(new { applied.Id, message = "Đã áp dụng chương trình cho học kỳ." });
    }

    [HttpPost("ap-dung-hoc-ky/{id:guid}/mo-lop")]
    public async Task<IActionResult> OpenClasses(Guid id, OpenClassesRequest request, CancellationToken ct)
    {
        var application = await db.ApDungHocKys.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (application is null) return NotFound();
        if (!await db.MonKeHoachDaoTaos.AnyAsync(x => x.KeHoachDaoTaoId == application.KeHoachDaoTaoId && x.ThuTuHocKy == application.ThuTuHocKy && x.MaMonHoc == request.MaMonHoc, ct))
            return BadRequest(new { message = "Môn không thuộc học kỳ được áp dụng." });
        var codes = TrainingRules.ClassCodes(request.TienToMaLop.Trim(), request.SoLop);
        if (await db.LopHocPhans.AnyAsync(x => codes.Contains(x.MaLopHocPhan), ct) || await db.LopHocPhanDuKiens.AnyAsync(x => codes.Contains(x.MaLopHocPhan), ct))
            return Conflict(new { message = "Có mã lớp trùng. Hãy đổi tiền tố; chưa có lớp nào được tạo." });
        db.LopHocPhanDuKiens.AddRange(codes.Select(code => new LopHocPhanDuKien
        { MaLopHocPhan = code, ApDungHocKyId = id, MaMonHoc = request.MaMonHoc, SiSoToiDa = request.SiSoToiDa }));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { return Conflict(new { message = "Mã lớp vừa được sử dụng. Chưa tạo lớp; hãy đổi tiền tố." }); }
        return Ok(new { maLopHocPhans = codes, message = $"Đã mở {codes.Length} lớp chờ phân công." });
    }

    [HttpPost("lop-hoc-phan/{code}/phan-cong")]
    public async Task<IActionResult> Assign(string code, AssignPlannedClassRequest request, CancellationToken ct)
    {
        // Execution strategy bao trọn transaction, tương thích EnableRetryOnFailure.
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var planned = await db.LopHocPhanDuKiens.Include(x => x.ApDung).ThenInclude(x => x.KeHoach)
                .SingleOrDefaultAsync(x => x.MaLopHocPhan == code, ct);
            if (planned is null) return (IActionResult)NotFound();
            if (planned.DaPhanCong || await db.LopHocPhans.AnyAsync(x => x.MaLopHocPhan == code, ct))
                return Conflict(new { message = "Lớp đã được phân công." });
            if (request.TietKetThuc < request.TietBatDau) return BadRequest(new { message = "Tiết kết thúc không được trước tiết bắt đầu." });
            var lecturer = await db.GiangViens.SingleOrDefaultAsync(x => x.MaGiangVien == request.MaGiangVien && x.TrangThaiTaiKhoan, ct);
            if (lecturer is null) return BadRequest(new { message = "Giảng viên không tồn tại hoặc bị khóa." });
            var major = planned.ApDung.KeHoach.MaNganh;
            var majorName = await db.Database.SqlQuery<string>($"SELECT tenNganh AS Value FROM NganhHoc WHERE maNganh={major}").FirstOrDefaultAsync(ct);
            if (!string.Equals(lecturer.ChuyenNganh?.Trim(), major, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(lecturer.ChuyenNganh?.Trim(), majorName, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Chuyên ngành giảng viên không khớp ngành của chương trình." });
            if (!await db.PhongHocs.AnyAsync(x => x.MaPhong == request.MaPhong, ct)) return BadRequest(new { message = "Phòng học không tồn tại." });
            var semesterCode = planned.ApDung.MaHocKy;
            if (await db.LichHocs.AnyAsync(x => x.LopHocPhan.MaHocKy == semesterCode && x.ThuTrongTuan == request.ThuTrongTuan &&
                x.TietBatDau <= request.TietKetThuc && request.TietBatDau <= x.TietKetThuc &&
                (x.MaPhong == request.MaPhong || x.LopHocPhan.MaGiangVien == request.MaGiangVien), ct))
                return Conflict(new { message = "Trùng lịch giảng viên hoặc phòng học trong học kỳ." });
            var semester = await db.HocKys.SingleAsync(x => x.MaHocKy == semesterCode, ct);
            if (semester.NgayBatDau is null || semester.NgayKetThuc is null || semester.NgayKetThuc < semester.NgayBatDau || (semester.NgayKetThuc.Value - semester.NgayBatDau.Value).TotalDays > 366)
                return BadRequest(new { message = "Khoảng ngày học kỳ không hợp lệ." });
            var dates = TrainingRules.SessionDates(semester.NgayBatDau.Value, semester.NgayKetThuc.Value, request.ThuTrongTuan).ToArray();
            if (dates.Length == 0) return BadRequest(new { message = "Học kỳ không có buổi học vào thứ đã chọn." });
            var schedule = $"LH-{code}";
            db.LopHocPhans.Add(new LopHocPhan { MaLopHocPhan = code, MaMonHoc = planned.MaMonHoc, MaHocKy = semesterCode,
                MaGiangVien = request.MaGiangVien, SiSoToiDa = planned.SiSoToiDa, TrangThai = "1" });
            db.LichHocs.Add(new LichHoc { MaLichHoc = schedule, MaLopHocPhan = code, MaPhong = request.MaPhong,
                ThuTrongTuan = request.ThuTrongTuan, TietBatDau = request.TietBatDau, TietKetThuc = request.TietKetThuc });
            db.BuoiHocs.AddRange(dates.Select(date => new BuoiHoc { MaBuoiHoc = $"BH-{code}-{date:yyyyMMdd}",
                MaLichHoc = schedule, NgayHocCuThe = date, TrangThaiBuoiHoc = "ChuaDienRa" }));
            planned.DaPhanCong = true;
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return Ok(new { message = "Đã phân công và tạo lịch học. Lớp chưa mở đăng ký." });
        });
    }

    [HttpPost("lop-hoc-phan/{code}/mo-dang-ky")]
    public async Task<IActionResult> Publish(string code, CancellationToken ct)
    {
        var course = await db.LopHocPhans.Include(x => x.LichHocs).SingleOrDefaultAsync(x => x.MaLopHocPhan == code, ct);
        if (course is null || !await db.LopHocPhanDuKiens.AnyAsync(x => x.MaLopHocPhan == code && x.DaPhanCong, ct)) return NotFound();
        if (course.LichHocs.Count == 0 || string.IsNullOrWhiteSpace(course.MaGiangVien))
            return BadRequest(new { message = "Cần phân công giảng viên và lịch học trước khi mở đăng ký." });
        course.TrangThai = "0"; await db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã mở đăng ký lớp học phần." });
    }

    private static bool IsDuplicate(DbUpdateException ex) => ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627;
}
