using System.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Faces;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

[ApiController, Route("api/v1/qr-diem-danh"), Authorize, EnableRateLimiting("face-recognition")]
public sealed class QrAttendanceController(AppDbContext db, AttendanceQrTokens qr, IFaceEvidenceService evidence,
    IFaceRecognitionService faces, FaceReviewPhotos reviewPhotos, IFacePhotoStorage photoStorage,
    ILogger<QrAttendanceController> logger) : ControllerBase
{
    private const string Managers = RoleNames.GiangVien + "," + RoleNames.Admin + "," + RoleNames.CanBoQuanLy;
    private const string FaceApprovers = RoleNames.Admin + "," + RoleNames.CanBoQuanLy;
    private string Student => User.Identity!.Name!;
    private bool IsManager => User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.CanBoQuanLy);
    private IQueryable<BuoiHoc> ManagedLessons() => db.BuoiHocs.Where(x => IsManager || x.LichHoc.LopHocPhan.MaGiangVien == Student);
    private ObjectResult Error(string code, string message, int status = 422) => StatusCode(status, new { code, message });
    private ObjectResult AlreadyRegistered(bool approved) => Error(approved ? "ProfileLocked" : "ProfilePendingApproval",
        approved ? "Hồ sơ khuôn mặt đã được duyệt và dùng chung cho tất cả lớp. Không cần đăng ký lại."
            : "Bạn đã đăng ký khuôn mặt và đang chờ duyệt một lần để dùng cho tất cả lớp. Không cần đăng ký lại.", 409);

    [HttpPost("huong-dan"), EnableRateLimiting("face-guidance"), RequestSizeLimit(1_500_000)]
    public async Task<IActionResult> Guidance(FaceGuidanceRequest request, CancellationToken ct)
    {
        var challenge = await db.AttendanceChallenges.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == request.ChallengeId && x.IssuedTo == Student && x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow, ct);
        if (challenge is null) return Error("ChallengeExpired", "Lượt xác minh đã hết hạn hoặc không thuộc tài khoản này.", 409);
        if (challenge.Purpose == "Approval" && !IsManager) return Forbid();
        if (challenge.Purpose == "Approval") return Error("PhotoReviewRequired", "Duyệt hồ sơ bằng đối chiếu ảnh thẻ với ảnh đăng ký, không chụp lại trước camera.", 410);
        try { return Ok(await evidence.InspectPoseAsync(request.Frame, ct)); }
        catch (FaceEvidenceException e) { return Error(e.Code, e.Message, e.Code == "AiServiceUnavailable" ? 503 : 422); }
    }

    [HttpGet("buoi-hoc"), Authorize(Roles = Managers)]
    public async Task<IActionResult> Lessons(CancellationToken ct)
    {
        var from = DateTime.UtcNow.AddHours(7).Date.AddDays(-7); var to = from.AddDays(22);
        var rows = await ManagedLessons().AsNoTracking().Where(x => x.NgayHocCuThe >= from && x.NgayHocCuThe < to)
            .OrderBy(x => x.NgayHocCuThe).ThenBy(x => x.MaBuoiHoc)
            .Select(x => new AttendanceSessionSummary(x.MaBuoiHoc, x.LichHoc.MaLopHocPhan,
                x.LichHoc.LopHocPhan.MonHoc.TenMonHoc, x.NgayHocCuThe, x.TrangThaiBuoiHoc == "DaChot",
                db.PhienDiemDanhs.Where(p => p.MaBuoiHoc == x.MaBuoiHoc && p.DongLucUtc == null && p.HetHanLucUtc > DateTime.UtcNow)
                    .OrderByDescending(p => p.MoLucUtc).Select(p => (Guid?)p.Id).FirstOrDefault(),
                db.PhienDiemDanhs.Where(p => p.MaBuoiHoc == x.MaBuoiHoc && p.DongLucUtc == null && p.HetHanLucUtc > DateTime.UtcNow)
                    .OrderByDescending(p => p.MoLucUtc).Select(p => (DateTime?)p.HetHanLucUtc).FirstOrDefault()))
            .Take(300).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("buoi-hoc/{code}/mo"), Authorize(Roles = Managers)]
    public async Task<IActionResult> Open(string code, OpenAttendanceRequest request, CancellationToken ct)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var lesson = await ManagedLessons().SingleOrDefaultAsync(x => x.MaBuoiHoc == code, ct);
            if (lesson is null) return Error("LessonNotFound", "Không tìm thấy buổi học thuộc quyền phụ trách.", 404);
            if (lesson.TrangThaiBuoiHoc == "DaChot") return Error("AttendanceFinalized", "Buổi học đã chốt.", 409);
            var now = DateTime.UtcNow;
            lesson.TrangThaiBuoiHoc = "DangDienRa";
            lesson.ThoiGianDiemDanh = now;
            var existing = await db.PhienDiemDanhs.Where(x => x.MaBuoiHoc == code && x.DongLucUtc == null).ToListAsync(ct);
            foreach (var old in existing) old.DongLucUtc = now;
            var session = new PhienDiemDanh { MaBuoiHoc = code, QrTokenHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                MoLucUtc = now, HetHanLucUtc = now.AddMinutes(request.Minutes) };
            db.PhienDiemDanhs.Add(session);
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return (IActionResult)Ok(new { sessionId = session.Id });
        });
    }

    [HttpGet("phien/{id:guid}/qr"), Authorize(Roles = Managers)]
    public async Task<IActionResult> GetQr(Guid id, CancellationToken ct)
    {
        var session = await db.PhienDiemDanhs.AsNoTracking().Include(x => x.BuoiHoc).ThenInclude(x => x.LichHoc)
            .ThenInclude(x => x.LopHocPhan).ThenInclude(x => x.MonHoc).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (session is null) return NotFound();
        if (!await ManagedLessons().AnyAsync(x => x.MaBuoiHoc == session.MaBuoiHoc, ct)) return Forbid();
        if (!session.ConHieuLuc || session.BuoiHoc.TrangThaiBuoiHoc == "DaChot") return Error("SessionClosed", "Phiên điểm danh đã đóng hoặc hết hạn.", 409);
        var sessionExpires = DateTime.SpecifyKind(session.HetHanLucUtc, DateTimeKind.Utc);
        var expires = new[] { DateTime.UtcNow.AddSeconds(30), sessionExpires }.Min();
        Response.Headers.CacheControl = "no-store";
        return Ok(new AttendanceQrResponse(id, session.MaBuoiHoc, session.BuoiHoc.LichHoc.MaLopHocPhan,
            session.BuoiHoc.LichHoc.LopHocPhan.MonHoc.TenMonHoc, sessionExpires,
            qr.Issue(id, session.QrTokenHash, expires), expires));
    }

    [HttpPost("phien/{id:guid}/dong"), Authorize(Roles = Managers)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var session = await db.PhienDiemDanhs.FindAsync([id], ct);
        if (session is null) return NotFound();
        if (!await ManagedLessons().AnyAsync(x => x.MaBuoiHoc == session.MaBuoiHoc, ct)) return Forbid();
        session.DongLucUtc ??= DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("phien/{id:guid}/ket-qua"), Authorize(Roles = Managers)]
    public async Task<IActionResult> Results(Guid id, CancellationToken ct)
    {
        var session = await db.PhienDiemDanhs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (session is null) return NotFound();
        if (!await ManagedLessons().AnyAsync(x => x.MaBuoiHoc == session.MaBuoiHoc, ct)) return Forbid();
        return Ok(await db.ChiTietDiemDanhs.AsNoTracking().Where(x => x.MaBuoiHoc == session.MaBuoiHoc)
            .OrderByDescending(x => x.ThoiGianCheckIn).Select(x => new { x.MaSinhVien, x.SinhVien.HoTen, x.TrangThai, x.ThoiGianCheckIn }).ToListAsync(ct));
    }

    [HttpPost("quet"), Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> Scan(ScanAttendanceQrRequest request, CancellationToken ct)
    {
        var payload = qr.Read(request.QrToken, DateTime.UtcNow);
        if (payload is null) return Error("QrExpired", "QR không hợp lệ hoặc đã hết hạn. Quét mã mới trên màn hình giảng viên.");
        var session = await db.PhienDiemDanhs.AsNoTracking().Include(x => x.BuoiHoc).ThenInclude(x => x.LichHoc)
            .ThenInclude(x => x.LopHocPhan).ThenInclude(x => x.MonHoc).SingleOrDefaultAsync(x => x.Id == payload.SessionId, ct);
        if (session is null || session.QrTokenHash != payload.SessionStamp || !session.ConHieuLuc || session.BuoiHoc.TrangThaiBuoiHoc == "DaChot")
            return Error("SessionClosed", "Phiên điểm danh đã đóng hoặc hết hạn.");
        var classCode = session.BuoiHoc.LichHoc.MaLopHocPhan;
        if (!await db.DanhSachSinhVienLops.AnyAsync(x => x.MaSinhVien == Student && x.MaLopHocPhan == classCode, ct))
            return Error("NotEnrolled", "Tài khoản sinh viên không thuộc lớp học phần này.", 403);
        if (await CheckedIn(session.MaBuoiHoc, ct)) return Error("AlreadyCheckedIn", "Bạn đã điểm danh buổi này.", 409);
        var faceProfile = await db.HoSoKhuonMats.AsNoTracking().Where(x => x.MaSinhVien == Student)
            .Select(x => new { x.ApprovedAtUtc, x.RejectedAtUtc, x.RejectionReason }).SingleOrDefaultAsync(ct);
        if (faceProfile is null)
            return Error("FaceProfileMissing", "Bạn chưa đăng ký khuôn mặt. Đăng ký và được duyệt một lần để dùng cho tất cả lớp.");
        if (faceProfile.RejectedAtUtc != null) return Error("FaceProfileRejected", $"Hồ sơ cần đăng ký lại: {faceProfile.RejectionReason}");
        if (faceProfile.ApprovedAtUtc is null)
            return Error("FaceProfileNotApproved", "Bạn đã đăng ký khuôn mặt và đang chờ duyệt. Không cần đăng ký lại cho lớp này; hồ sơ được dùng chung cho tất cả lớp.");
        var student = await db.SinhViens.AsNoTracking().SingleAsync(x => x.MaSinhVien == Student, ct);
        var challenge = await CreateChallenge("Attendance", session.Id, session.HetHanLucUtc, ct);
        return Ok(new FaceChallengeResponse(challenge.Id, challenge.ExpiresAtUtc, challenge.Direction, Student, student.HoTen,
            session.MaBuoiHoc, classCode, session.BuoiHoc.LichHoc.LopHocPhan.MonHoc.TenMonHoc));
    }

    [HttpPost("xac-nhan"), Authorize(Roles = RoleNames.SinhVien), RequestSizeLimit(4_500_000)]
    public async Task<IActionResult> Confirm(FaceEvidenceRequest request, CancellationToken ct)
    {
        var challenge = await ClaimChallenge(request.ChallengeId, "Attendance", ct);
        if (challenge is null) return Error("ChallengeExpired", "Lượt xác minh đã dùng hoặc hết hạn. Vui lòng quét QR lại.", 409);
        try
        {
            await BindEvidence(challenge, request.Frames, ct);
            var samples = await evidence.AnalyzeAsync(request.Frames, challenge.Direction, ct);
            var normalized = faces.NormalizeSamples(samples);
            if (!normalized.Succeeded) return Error(normalized.ErrorCode!, normalized.ErrorMessage!);
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var session = await db.PhienDiemDanhs.Include(x => x.BuoiHoc).ThenInclude(x => x.LichHoc)
                    .SingleOrDefaultAsync(x => x.Id == challenge.PhienDiemDanhId, ct);
                if (session is null || !session.ConHieuLuc || session.BuoiHoc.TrangThaiBuoiHoc == "DaChot" || challenge.ExpiresAtUtc <= DateTime.UtcNow)
                    return Error("SessionClosed", "Phiên hoặc lượt xác minh đã hết hạn.", 409);
                if (!await db.DanhSachSinhVienLops.AnyAsync(x => x.MaSinhVien == Student && x.MaLopHocPhan == session.BuoiHoc.LichHoc.MaLopHocPhan, ct))
                    return Error("NotEnrolled", "Sinh viên không còn thuộc lớp.", 403);
                if (!await db.HoSoKhuonMats.AnyAsync(x => x.MaSinhVien == Student && x.ApprovedAtUtc != null, ct))
                    return Error("FaceProfileNotApproved", "Hồ sơ khuôn mặt chưa được duyệt.");
                // Verify every frame against this account, rather than searching the class roster.
                foreach (var sample in samples)
                    if (!await faces.VerifyStudentAsync(Student, sample, ct)) return Error("FaceNotMatched", "Khuôn mặt không khớp tài khoản đang đăng nhập.");
                var record = await db.ChiTietDiemDanhs.SingleOrDefaultAsync(x => x.MaBuoiHoc == session.MaBuoiHoc && x.MaSinhVien == Student, ct);
                if (record?.TrangThai is "CoMat" or "DiTre") return Error("AlreadyCheckedIn", "Bạn đã điểm danh buổi này.", 409);
                if (record is null) { record = new() { MaBuoiHoc = session.MaBuoiHoc, MaSinhVien = Student }; db.ChiTietDiemDanhs.Add(record); }
                record.TrangThai = "CoMat"; record.ThoiGianCheckIn = DateTime.UtcNow;
                await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
                logger.LogInformation("QR attendance accepted for {StudentCode}, lesson {LessonCode}, challenge {ChallengeId}", Student, session.MaBuoiHoc, challenge.Id);
                return (IActionResult)Ok(new { maSinhVien = Student, maBuoiHoc = session.MaBuoiHoc, trangThai = record.TrangThai, thoiGianCheckInUtc = record.ThoiGianCheckIn });
            });
        }
        catch (FaceEvidenceException e)
        {
            logger.LogInformation("QR attendance rejected for {StudentCode}, challenge {ChallengeId}: {Code}", Student, challenge.Id, e.Code);
            return Error(e.Code, e.Message, e.Code == "AiServiceUnavailable" ? 503 : 422);
        }
    }

    [HttpGet("ho-so"), Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var profile = await db.HoSoKhuonMats.AsNoTracking().Where(x => x.MaSinhVien == Student)
            .Select(x => new { x.ApprovedAtUtc, x.RejectedAtUtc, x.RejectionReason, x.ThoiGianCapNhat }).SingleOrDefaultAsync(ct);
        return Ok(new { maSinhVien = Student, daDangKy = profile != null, daDuyet = profile?.ApprovedAtUtc != null,
            biTuChoi = profile?.RejectedAtUtc != null, lyDoTuChoi = profile?.RejectionReason, thoiGianCapNhat = profile?.ThoiGianCapNhat });
    }

    [HttpGet("ket-qua-cua-toi"), Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> MyResults(CancellationToken ct) => Ok(await db.ChiTietDiemDanhs.AsNoTracking()
        .Where(x => x.MaSinhVien == Student).OrderByDescending(x => x.BuoiHoc.NgayHocCuThe).ThenBy(x => x.MaBuoiHoc)
        .Select(x => new { x.MaBuoiHoc, x.BuoiHoc.LichHoc.MaLopHocPhan, x.BuoiHoc.LichHoc.LopHocPhan.MonHoc.TenMonHoc,
            ngayHoc = x.BuoiHoc.NgayHocCuThe, x.TrangThai, thoiGianCheckInUtc = x.ThoiGianCheckIn }).Take(100).ToListAsync(ct));

    [HttpGet("buoi-hoc-cua-toi"), Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> MyLessons(CancellationToken ct)
    {
        var date = DateTime.UtcNow.AddHours(7).Date; var next = date.AddDays(1);
        return Ok(await db.BuoiHocs.AsNoTracking().Where(x => x.NgayHocCuThe >= date && x.NgayHocCuThe < next
            && db.DanhSachSinhVienLops.Any(e => e.MaSinhVien == Student && e.MaLopHocPhan == x.LichHoc.MaLopHocPhan))
            .OrderBy(x => x.LichHoc.TietBatDau).Select(x => new { x.MaBuoiHoc, x.LichHoc.MaLopHocPhan,
                x.LichHoc.LopHocPhan.MaMonHoc, x.LichHoc.LopHocPhan.MonHoc.TenMonHoc,
                giangVien = x.LichHoc.LopHocPhan.GiangVien.HoTen, x.LichHoc.TietBatDau, x.LichHoc.TietKetThuc,
                phong = x.LichHoc.MaPhong, x.TrangThaiBuoiHoc,
                daDiemDanh = db.ChiTietDiemDanhs.Any(d => d.MaBuoiHoc == x.MaBuoiHoc && d.MaSinhVien == Student && (d.TrangThai == "CoMat" || d.TrangThai == "DiTre")),
                thoiGianDiemDanhUtc = db.ChiTietDiemDanhs.Where(d => d.MaBuoiHoc == x.MaBuoiHoc && d.MaSinhVien == Student).Select(d => d.ThoiGianCheckIn).FirstOrDefault() }).ToListAsync(ct));
    }

    [HttpPost("ho-so/thu-thach"), Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> EnrollmentChallenge(CancellationToken ct)
    {
        var student = await db.SinhViens.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == Student, ct);
        if (student is null) return NotFound();
        var existing = await db.HoSoKhuonMats.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == Student, ct);
        if (existing is not null && existing.RejectedAtUtc == null) return AlreadyRegistered(existing.ApprovedAtUtc != null);
        var challenge = await CreateChallenge("Enrollment", null, DateTime.UtcNow.AddMinutes(2), ct);
        return Ok(new FaceChallengeResponse(challenge.Id, challenge.ExpiresAtUtc, challenge.Direction, Student, student.HoTen, null, null, null));
    }

    [HttpPost("ho-so"), Authorize(Roles = RoleNames.SinhVien), RequestSizeLimit(4_500_000)]
    public async Task<IActionResult> Enroll(FaceEvidenceRequest request, CancellationToken ct)
    {
        if (!request.Consent || !request.StoreReviewPhotosConsent) return Error("ConsentRequired", "Cần đồng ý lưu ảnh đăng ký và mẫu khuôn mặt mã hóa để CBQL/Admin đối chiếu và phục vụ điểm danh. Vui lòng tải bản ứng dụng mới.");
        var existing = await db.HoSoKhuonMats.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == Student, ct);
        if (existing is not null && existing.RejectedAtUtc == null) return AlreadyRegistered(existing.ApprovedAtUtc != null);
        var challenge = await ClaimChallenge(request.ChallengeId, "Enrollment", ct);
        if (challenge is null) return Error("ChallengeExpired", "Lượt đăng ký đã hết hạn hoặc đã dùng. Hãy bắt đầu lại.", 409);
        try
        {
            await BindEvidence(challenge, request.Frames, ct);
            var samples = await evidence.AnalyzeAsync(request.Frames, challenge.Direction, ct);
            var normalized = faces.NormalizeSamples(samples);
            if (!normalized.Succeeded) return Error(normalized.ErrorCode!, normalized.ErrorMessage!);
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var profile = await db.HoSoKhuonMats.SingleOrDefaultAsync(x => x.MaSinhVien == Student, ct);
                if (profile is not null && profile.RejectedAtUtc == null) return AlreadyRegistered(profile.ApprovedAtUtc != null);
                if (challenge.ExpiresAtUtc <= DateTime.UtcNow) return Error("ChallengeExpired", "Lượt đăng ký đã hết hạn.", 409);
                if (profile is null) { profile = new() { MaHoSo = $"FACE-{Guid.NewGuid():N}", MaSinhVien = Student }; db.HoSoKhuonMats.Add(profile); }
                profile.VectorJson = faces.ProtectDescriptor(normalized.Value!); profile.ThoiGianCapNhat = DateTime.UtcNow;
                // Ảnh của lần đăng ký trước không còn dùng nữa, dọn trước khi ghi đè mã ảnh mới.
                await photoStorage.DeleteRegistrationAsync(profile.RegistrationPhotosProtected, ct);
                profile.RegistrationPhotosProtected = await photoStorage.SaveRegistrationAsync(request.Frames, ct); profile.ReviewVersion = Guid.NewGuid();
                profile.RejectedAtUtc = null; profile.RejectedBy = null; profile.RejectionReason = null; profile.ReviewNote = null;
                profile.ApprovedAtUtc = null; profile.ApprovedBy = null;
                await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
                return (IActionResult)Ok(new { message = "Đã đăng ký khuôn mặt dùng chung cho tất cả lớp. Chờ CBQL hoặc Admin xác minh và duyệt một lần." });
            });
        }
        catch (FaceEvidenceException e)
        {
            logger.LogInformation("Face enrollment rejected for {StudentCode}, challenge {ChallengeId}: {Code}", Student, challenge.Id, e.Code);
            return Error(e.Code, e.Message, e.Code == "AiServiceUnavailable" ? 503 : 422);
        }
    }

    private IQueryable<HoSoKhuonMat> ManagedProfiles() => db.HoSoKhuonMats.Where(x => IsManager);

    [HttpGet("ho-so/cho-duyet"), Authorize(Roles = FaceApprovers)]
    public async Task<IActionResult> Pending(CancellationToken ct) {
        if (!IsManager) return Forbid();
        return Ok(await ManagedProfiles().AsNoTracking()
        .OrderBy(x => x.ApprovedAtUtc).ThenByDescending(x => x.ThoiGianCapNhat)
        .Select(x => new { x.MaSinhVien, x.SinhVien.HoTen, x.ThoiGianCapNhat, daDuyet = x.ApprovedAtUtc != null,
            biTuChoi = x.RejectedAtUtc != null, lyDoTuChoi = x.RejectionReason }).Take(500).ToListAsync(ct)); }

    [HttpGet("ho-so/{code}/doi-chieu"), Authorize(Roles = FaceApprovers)]
    public async Task<IActionResult> ReviewDetail(string code, CancellationToken ct)
    {
        if (!IsManager) return Forbid();
        var profile = await ManagedProfiles().AsNoTracking().Include(x => x.SinhVien).SingleOrDefaultAsync(x => x.MaSinhVien == code, ct);
        if (profile is null) return NotFound();
        var photo = await db.StudentIdentityPhotos.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == code, ct);
        var identity = reviewPhotos.ReadIdentity(photo?.ImageProtected);
        var registration = await photoStorage.ReadRegistrationAsync(profile.RegistrationPhotosProtected, ct);
        Response.Headers.CacheControl = "no-store, private";
        return Ok(new { maSinhVien = code, profile.SinhVien.HoTen, profile.SinhVien.LopHoc, profile.ThoiGianCapNhat,
            profileVersion = profile.ReviewVersion, identityPhotoVersion = photo?.Version,
            anhThe = identity is null ? null : FaceReviewPhotos.DataUrl(identity),
            anhDangKy = registration,
            anhTheCapNhatBoi = photo?.UpdatedBy, anhTheCapNhatLucUtc = photo?.UpdatedAtUtc,
            daDuyet = profile.ApprovedAtUtc != null, biTuChoi = profile.RejectedAtUtc != null,
            nguoiDuyet = profile.ApprovedBy, ngayDuyet = profile.ApprovedAtUtc,
            lyDoTuChoi = profile.RejectionReason, ghiChu = profile.ReviewNote });
    }

    [HttpPost("ho-so/{code}/anh-the"), Authorize(Roles = FaceApprovers), RequestSizeLimit(1_500_000)]
    public async Task<IActionResult> SetIdentityPhoto(string code, IdentityPhotoRequest request, CancellationToken ct)
    {
        if (!IsManager) return Forbid();
        if (!await db.SinhViens.AnyAsync(x => x.MaSinhVien == code, ct)) return NotFound();
        string image;
        try { image = reviewPhotos.ProtectIdentity(request.Frame); }
        catch (FaceEvidenceException e) { return Error(e.Code, e.Message); }
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (await db.HoSoKhuonMats.AnyAsync(x => x.MaSinhVien == code && x.ApprovedAtUtc != null, ct))
                return Error("ProfileLocked", "Hồ sơ đã được duyệt. Cần hủy hồ sơ cũ trước khi thay ảnh thẻ làm căn cứ xác minh.", 409);
            var photo = await db.StudentIdentityPhotos.SingleOrDefaultAsync(x => x.MaSinhVien == code, ct);
            if (photo is null) { photo = new() { MaSinhVien = code }; db.StudentIdentityPhotos.Add(photo); }
            photo.ImageProtected = image; photo.Version = Guid.NewGuid(); photo.UpdatedAtUtc = DateTime.UtcNow; photo.UpdatedBy = Student;
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return (IActionResult)Ok(new { message = "Đã cập nhật ảnh thẻ từ hồ sơ gốc.", identityPhotoVersion = photo.Version });
        });
    }

    [HttpPost("ho-so/{code}/duyet"), Authorize(Roles = FaceApprovers)]
    public Task<IActionResult> Approve(string code, FacePhotoReviewRequest request, CancellationToken ct) => DecideReview(code, request, true, ct);

    [HttpPost("ho-so/{code}/tu-choi"), Authorize(Roles = FaceApprovers)]
    public Task<IActionResult> Reject(string code, FacePhotoReviewRequest request, CancellationToken ct) => DecideReview(code, request, false, ct);

    private async Task<IActionResult> DecideReview(string code, FacePhotoReviewRequest request, bool approve, CancellationToken ct)
    {
        if (!IsManager) return Forbid();
        if (approve && !request.ConfirmCompared) return Error("ComparisonRequired", "Cần xác nhận đã đối chiếu ảnh thẻ với ảnh chụp đăng ký.");
        var note = request.Note?.Trim();
        if (!approve && string.IsNullOrEmpty(note)) return Error("RejectionReasonRequired", "Nhập lý do yêu cầu sinh viên đăng ký lại.");
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var profile = await db.HoSoKhuonMats.SingleOrDefaultAsync(x => x.MaSinhVien == code, ct);
            if (profile is null) return NotFound();
            if (profile.ApprovedAtUtc != null) return AlreadyRegistered(true);
            if (profile.RejectedAtUtc != null) return Error("ProfileReturned", "Hồ sơ đã được trả lại, đang chờ sinh viên đăng ký lại.", 409);
            var photo = await db.StudentIdentityPhotos.SingleOrDefaultAsync(x => x.MaSinhVien == code, ct);
            if (profile.ReviewVersion != request.ProfileVersion || photo?.Version != request.IdentityPhotoVersion)
                return Error("ReviewChanged", "Ảnh hoặc hồ sơ vừa thay đổi. Tải lại để đối chiếu trước khi quyết định.", 409);
            if (approve && (await photoStorage.CountRegistrationAsync(profile.RegistrationPhotosProtected, ct) != 3 || reviewPhotos.ReadIdentity(photo?.ImageProtected) is null))
                return Error("ReviewEvidenceMissing", "Cần đủ ảnh thẻ từ hồ sơ gốc và ba ảnh đăng ký để duyệt. Hồ sơ cũ thiếu ảnh cần được trả lại để sinh viên đăng ký lại.");
            profile.ReviewNote = note;
            if (approve) { profile.ApprovedAtUtc = DateTime.UtcNow; profile.ApprovedBy = Student; }
            else { profile.RejectedAtUtc = DateTime.UtcNow; profile.RejectedBy = Student; profile.RejectionReason = note; }
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            logger.LogInformation("Face photo review {Decision} for {StudentCode} by {Actor}", approve ? "approved" : "returned", code, Student);
            return (IActionResult)Ok(new { message = approve ? "Đã duyệt hồ sơ khuôn mặt dùng chung cho tất cả lớp." : "Đã trả hồ sơ kèm lý do. Sinh viên có thể đăng ký lại." });
        });
    }

    [HttpPost("ho-so/{code}/thu-thach-duyet"), Authorize(Roles = FaceApprovers)]
    public Task<IActionResult> ApprovalChallenge(string code, CancellationToken ct)
    {
        return Task.FromResult<IActionResult>(!IsManager ? Forbid() : Error("PhotoReviewRequired", "Duyệt bằng đối chiếu ảnh thẻ và ảnh đăng ký. Không yêu cầu sinh viên chụp lại tại máy người duyệt.", 410));
    }

    [HttpPost("ho-so/{code}/huy"), Authorize(Roles = FaceApprovers)]
    public async Task<IActionResult> Revoke(string code, CancellationToken ct)
    {
        if (!IsManager) return Forbid();
        var deleted = await ManagedProfiles().Where(x => x.MaSinhVien == code).ExecuteDeleteAsync(ct);
        return deleted == 1 ? NoContent() : NotFound();
    }

    private async Task BindEvidence(AttendanceChallenge challenge, string[] frames, CancellationToken ct)
    {
        if (frames is null || frames.Length != 3 || frames.Any(x => string.IsNullOrEmpty(x) || x.Length > 1_400_000))
            throw new FaceEvidenceException("InvalidEvidence", "Cần ba ảnh camera, mỗi ảnh tối đa 1 MB.");
        string[] hashes;
        try { hashes = frames.Select(x => Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(x)))).ToArray(); }
        catch (FormatException) { throw new FaceEvidenceException("InvalidImage", "Dữ liệu ảnh không hợp lệ."); }
        if (hashes.Distinct(StringComparer.Ordinal).Count() != 3)
            throw new FaceEvidenceException("EvidenceReplayed", "Ảnh trùng nhau. Hãy chụp lại trực tiếp từ camera.");
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var h0 = hashes[0]; var h1 = hashes[1]; var h2 = hashes[2];
            if (await db.AttendanceChallenges.AnyAsync(x => x.MaSinhVien == challenge.MaSinhVien && x.Id != challenge.Id
                && x.FrameHashes != null && (x.FrameHashes.Contains(h0) || x.FrameHashes.Contains(h1) || x.FrameHashes.Contains(h2)), ct))
                throw new FaceEvidenceException("EvidenceReplayed", "Ảnh đã được sử dụng trong lượt xác minh trước. Hãy chụp mới.");
            var row = await db.AttendanceChallenges.SingleAsync(x => x.Id == challenge.Id, ct);
            row.FrameHashes = string.Join('|', hashes);
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        });
    }
    private Task<bool> CheckedIn(string lesson, CancellationToken ct) => db.ChiTietDiemDanhs.AnyAsync(x => x.MaBuoiHoc == lesson && x.MaSinhVien == Student && (x.TrangThai == "CoMat" || x.TrangThai == "DiTre"), ct);
    private async Task<AttendanceChallenge> CreateChallenge(string purpose, Guid? session, DateTime limit, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var challenge = new AttendanceChallenge { MaSinhVien = Student, IssuedTo = Student, Purpose = purpose, PhienDiemDanhId = session,
            Direction = RandomNumberGenerator.GetInt32(2) == 0 ? "left" : "right", CreatedAtUtc = now,
            ExpiresAtUtc = new[] { now.AddSeconds(90), DateTime.SpecifyKind(limit, DateTimeKind.Utc) }.Min() };
        db.AttendanceChallenges.Add(challenge); await db.SaveChangesAsync(ct); return challenge;
    }
    private async Task<AttendanceChallenge?> ClaimChallenge(Guid id, string purpose, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var count = await db.AttendanceChallenges.Where(x => x.Id == id && x.MaSinhVien == Student && x.Purpose == purpose && x.UsedAtUtc == null && x.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, now), ct);
        return count == 1 ? await db.AttendanceChallenges.AsNoTracking().SingleAsync(x => x.Id == id, ct) : null;
    }
}

