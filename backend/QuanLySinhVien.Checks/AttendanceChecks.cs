using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using QuanLySinhVien.Api.Contracts.Faces;
using QuanLySinhVien.Api.Controllers;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;
using SkiaSharp;

internal static class AttendanceChecks
{
    private static string Jpeg(byte shade) { using var bitmap = new SKBitmap(400, 400); bitmap.Erase(new SKColor(shade, shade, shade)); using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85); return Convert.ToBase64String(data.ToArray()); }
    private static string[] RegistrationFrames() => [Jpeg(40), Jpeg(80), Jpeg(120)];
    private static string[] FreshFrames() => Enumerable.Range(0, 3).Select(_ => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToArray();
    private static int _count;
    private static void Check(bool success, string label)
    { if (!success) throw new Exception(label); _count++; Console.WriteLine("PASS: " + label); }
    private static string? Code(IActionResult result) => (result as ObjectResult)?.Value?.GetType().GetProperty("code")?.GetValue((result as ObjectResult)?.Value)?.ToString();
    private static T Value<T>(IActionResult result) => (T)((OkObjectResult)result).Value!;
    public static async Task Run()
    {
        var provider = new EphemeralDataProtectionProvider(); var tokens = new AttendanceQrTokens(provider);
        var now = DateTime.UtcNow; var id = Guid.NewGuid(); var token = tokens.Issue(id, "stamp", now.AddSeconds(30));
        Check(tokens.Read(token, now)?.SessionId == id, "protected QR round-trip");
        Check(tokens.Read(token, now.AddSeconds(31)) == null, "expired QR rejected");
        Check(tokens.Read(token[..^4] + "xxxx", now) == null && tokens.Read("bad", now) == null, "tampered and foreign QR rejected");
        FaceEvidenceService.ValidateMotion([0.01, 0.20, 0.02], "left");
        FaceEvidenceService.ValidateMotion([0.01, -0.20, 0.02], "right");
        Check(true, "both motion directions supported");
        foreach (var poses in new double[][] { [0, 0, 0], [0, -0.20, 0], [0, 0.20, 0.5], [double.NaN, 0.2, 0] })
        {
            try { FaceEvidenceService.ValidateMotion(poses, "left"); throw new Exception("Invalid motion accepted"); }
            catch (FaceEvidenceException e) { Check(e.Code == "LivenessFailed", "stationary/wrong/incomplete/nonfinite motion rejected"); }
        }
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options); await db.Database.EnsureCreatedAsync();
        var recognition = new FaceRecognitionService(db, provider, Options.Create(new FaceRecognitionOptions()));
        var photos = new FaceReviewPhotos(provider);
        var vector = new double[512]; vector[0] = 1;
        var lesson = new BuoiHoc { MaBuoiHoc = "BH", NgayHocCuThe = now.Date, TrangThaiBuoiHoc = "DangDienRa",
            LichHoc = new LichHoc { MaLichHoc = "LH", MaPhong = "P", PhongHoc = new() { MaPhong = "P", TenPhong = "P" },
                LopHocPhan = new() { MaLopHocPhan = "LOP", MaHocKy = "HK", MonHoc = new() { MaMonHoc = "MH", TenMonHoc = "Test" },
                    GiangVien = new() { MaGiangVien = "GV", HoTen = "Lecturer" } } } };
        db.BuoiHocs.Add(lesson);
        db.SinhViens.AddRange(new SinhVien { MaSinhVien = "SV1", HoTen = "Student 1", Email = "s1@example.invalid", AnhDaiDien = "https://avatar.example.invalid/student.jpg" },
            new SinhVien { MaSinhVien = "SV2", HoTen = "Student 2", Email = "s2@example.invalid" });
        db.DanhSachSinhVienLops.Add(new() { MaSinhVien = "SV1", MaLopHocPhan = "LOP" });
        db.HoSoKhuonMats.Add(new() { MaHoSo = "FACE", MaSinhVien = "SV1", VectorJson = recognition.ProtectDescriptor(vector), ThoiGianCapNhat = now, ApprovedAtUtc = now,
            RegistrationPhotosProtected = photos.ProtectRegistration(RegistrationFrames()), ReviewVersion = Guid.NewGuid() });
        await db.SaveChangesAsync();
        var fake = new Evidence(vector);
        QrAttendanceController Controller(string name, string role)
        {
            var c = new QrAttendanceController(db, tokens, fake, recognition, photos, NullLogger<QrAttendanceController>.Instance);
            var services = new ServiceCollection().AddSingleton(Options.Create(new FaceRecognitionOptions())).BuildServiceProvider();
            c.ControllerContext = new() { HttpContext = new DefaultHttpContext { RequestServices = services,
                User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Name, name), new(ClaimTypes.Role, role)], "test")) } };
            return c;
        }
        var lecturer = Controller("GV", RoleNames.GiangVien); var student = Controller("SV1", RoleNames.SinhVien);
        var officer = Controller("CBQL", RoleNames.CanBoQuanLy);
        var admin = Controller("ADMIN", RoleNames.Admin);
        Check(await lecturer.Pending(default) is ForbidResult && await lecturer.ApprovalChallenge("SV1", default) is ForbidResult
            && await lecturer.Approve("SV1", new(), default) is ForbidResult && await lecturer.Revoke("SV1", default) is ForbidResult,
            "lecturer cannot list, issue approval challenge, approve or revoke face profiles");
        Check(await officer.Pending(default) is OkObjectResult && await admin.Pending(default) is OkObjectResult, "CBQL and Admin may manage global profiles without teaching the student");
        Check((await Controller("OTHER", RoleNames.GiangVien).Open("BH", new(10), default) as ObjectResult)?.StatusCode == 404, "another lecturer cannot open this lesson");
        await lecturer.Open("BH", new(10), default);
        var session = await db.PhienDiemDanhs.AsNoTracking().SingleAsync(x => x.DongLucUtc == null);
        var qr = Value<AttendanceQrResponse>(await lecturer.GetQr(session.Id, default));
        Check(qr.SessionExpiresAtUtc.Kind == DateTimeKind.Utc && qr.QrExpiresAtUtc.Kind == DateTimeKind.Utc,
            "QR response preserves UTC timezone after database read for browser countdown");
        Check(Code(await Controller("SV2", RoleNames.SinhVien).Scan(new(qr.QrToken), default)) == "NotEnrolled", "student outside class rejected before capture");
        var profile = await db.HoSoKhuonMats.SingleAsync(); profile.ApprovedAtUtc = null; await db.SaveChangesAsync();
        Check(Code(await student.Scan(new(qr.QrToken), default)) == "FaceProfileNotApproved", "unapproved enrollment rejected");
        var savedVector = profile.VectorJson;
        Check(Code(await student.EnrollmentChallenge(default)) == "ProfilePendingApproval", "pending global profile cannot start another enrollment");
        Check(Code(await student.Enroll(new() { ChallengeId = Guid.NewGuid(), Consent = true, StoreReviewPhotosConsent = true, Frames = FreshFrames() }, default)) == "ProfilePendingApproval"
            && fake.Calls == 0 && await db.HoSoKhuonMats.CountAsync(x => x.MaSinhVien == "SV1" && x.VectorJson == savedVector) == 1,
            "duplicate enrollment cannot overwrite pending profile or invoke AI");
        profile.ApprovedAtUtc = now; await db.SaveChangesAsync();
        var challenge = Value<FaceChallengeResponse>(await student.Scan(new(qr.QrToken), default));
        var guidance = new FaceGuidanceRequest { ChallengeId = challenge.ChallengeId, Frame = "fixture" };
        Check(Value<FacePoseObservation>(await student.Guidance(guidance, default)).Usable
            && !await db.ChiTietDiemDanhs.AnyAsync()
            && await db.AttendanceChallenges.AnyAsync(x => x.Id == challenge.ChallengeId && x.UsedAtUtc == null),
            "live guidance returns pose without consuming nonce or creating attendance");
        Check(Code(await Controller("SV2", RoleNames.SinhVien).Guidance(guidance, default)) == "ChallengeExpired", "live guidance cannot inspect another user's challenge");
        var request = new FaceEvidenceRequest { ChallengeId = challenge.ChallengeId, Frames = FreshFrames() };
        Check(Code(await Controller("SV2", RoleNames.SinhVien).Confirm(request, default)) == "ChallengeExpired", "challenge bound to authenticated student");
        var success = await student.Confirm(request, default);
        Check(success is OkObjectResult && await db.ChiTietDiemDanhs.CountAsync() == 1 && fake.Calls == 1, "valid account and live evidence produce one attendance row");
        Check(Code(await student.Guidance(guidance, default)) == "ChallengeExpired", "consumed nonce cannot request more live guidance");
        Check(Code(await student.Confirm(request, default)) == "ChallengeExpired" && fake.Calls == 1, "replay rejected before AI runs");
        Check(Code(await student.Scan(new(qr.QrToken), default)) == "AlreadyCheckedIn", "duplicate attendance rejected");
        await db.ChiTietDiemDanhs.ExecuteDeleteAsync();
        challenge = Value<FaceChallengeResponse>(await student.Scan(new(qr.QrToken), default));
        var previousCalls = fake.Calls;
        Check(Code(await student.Confirm(new() { ChallengeId = challenge.ChallengeId, Frames = request.Frames }, default)) == "EvidenceReplayed"
            && fake.Calls == previousCalls, "old photos with a fresh challenge are rejected before AI");
        challenge = Value<FaceChallengeResponse>(await student.Scan(new(qr.QrToken), default));
        request = new() { ChallengeId = challenge.ChallengeId, Frames = FreshFrames() };
        fake.BeforeReturn = async () => { await db.PhienDiemDanhs.Where(x => x.Id == session.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.DongLucUtc, DateTime.UtcNow)); };
        Check(Code(await student.Confirm(request, default)) == "SessionClosed" && !await db.ChiTietDiemDanhs.AnyAsync(), "closing session during inference prevents attendance");
        fake.BeforeReturn = null;
        await lecturer.Open("BH", new(10), default);
        session = await db.PhienDiemDanhs.AsNoTracking().SingleAsync(x => x.DongLucUtc == null);
        qr = Value<AttendanceQrResponse>(await lecturer.GetQr(session.Id, default));
        challenge = Value<FaceChallengeResponse>(await student.Scan(new(qr.QrToken), default));
        request = new() { ChallengeId = challenge.ChallengeId, Frames = FreshFrames() };
        fake.FailCode = "SpoofDetected";
        Check(Code(await student.Confirm(request, default)) == "SpoofDetected" && !await db.ChiTietDiemDanhs.AnyAsync(), "spoof evidence never creates attendance");
        fake.FailCode = null; fake.Vector = new double[512]; fake.Vector[1] = 1;
        challenge = Value<FaceChallengeResponse>(await student.Scan(new(qr.QrToken), default));
        Check(Code(await student.Confirm(new() { ChallengeId = challenge.ChallengeId, Frames = FreshFrames() }, default)) == "FaceNotMatched", "another face on valid account rejected");
        Check(Code(await student.EnrollmentChallenge(default)) == "ProfileLocked", "approved face cannot be replaced by student");
        Check(Code(await officer.ApprovalChallenge("SV1", default)) == "PhotoReviewRequired", "approval no longer starts a second camera challenge");
        profile = await db.HoSoKhuonMats.SingleAsync(x => x.MaSinhVien == "SV1");
        profile.ApprovedAtUtc = null; await db.SaveChangesAsync();
        Check(await lecturer.ReviewDetail("SV1", default) is ForbidResult
            && await student.SetIdentityPhoto("SV1", new() { Frame = Jpeg(60) }, default) is ForbidResult,
            "lecturers cannot view comparison photos and students cannot supply official reference photos");
        var version = profile.ReviewVersion;
        Check(Code(await officer.Approve("SV1", new() { ProfileVersion = version }, default)) == "ComparisonRequired", "approval requires explicit staff photo-comparison confirmation");
        Check(Code(await officer.Approve("SV1", new() { ProfileVersion = version, ConfirmCompared = true }, default)) == "ReviewEvidenceMissing", "missing official portrait blocks approval rather than using the avatar");
        Check(Code(await officer.SetIdentityPhoto("SV1", new() { Frame = "bad" }, default)) == "InvalidIdentityPhoto", "invalid reference image rejected");
        Check(await officer.SetIdentityPhoto("SV1", new() { Frame = Jpeg(60) }, default) is OkObjectResult, "CBQL uploads a private official portrait from the student record");
        var reference = await db.StudentIdentityPhotos.AsNoTracking().SingleAsync(x => x.MaSinhVien == "SV1");
        Check(reference.ImageProtected != Jpeg(60) && reference.UpdatedBy == "CBQL" && photos.ReadIdentity(reference.ImageProtected) is not null, "official photo encrypted with staff provenance");
        var detail = await officer.ReviewDetail("SV1", default);
        Check(detail is OkObjectResult && !System.Text.Json.JsonSerializer.Serialize(((OkObjectResult)detail).Value).Contains("VectorJson"), "staff comparison detail excludes recognition embedding");
        var decision = new FacePhotoReviewRequest { ProfileVersion = version, IdentityPhotoVersion = reference.Version, ConfirmCompared = true };
        Check(Code(await officer.Approve("SV1", new() { ProfileVersion = Guid.NewGuid(), IdentityPhotoVersion = reference.Version, ConfirmCompared = true }, default)) == "ReviewChanged", "stale enrollment version cannot be approved");
        await officer.SetIdentityPhoto("SV1", new() { Frame = Jpeg(65) }, default);
        Check(Code(await officer.Approve("SV1", decision, default)) == "ReviewChanged", "changed official photo invalidates an already-open comparison");
        reference = await db.StudentIdentityPhotos.AsNoTracking().SingleAsync(x => x.MaSinhVien == "SV1");
        decision = new() { ProfileVersion = version, IdentityPhotoVersion = reference.Version, ConfirmCompared = true };
        var aiCallsBeforeReview = fake.Calls;
        Check(await lecturer.Approve("SV1", decision, default) is ForbidResult, "lecturer cannot approve a photo comparison");
        Check(Code(await student.Scan(new("invalid"), default)) == "QrExpired", "malformed QR rejected");
        fake.Vector = vector;
        var approved = await officer.Approve("SV1", decision, default);
        Check(approved is OkObjectResult && fake.Calls == aiCallsBeforeReview, "CBQL approves stored-photo comparison without student recapture or extra live inference");
        Check(Code(await officer.Approve("SV1", decision, default)) == "ProfileLocked", "approved global profile cannot be approved twice");
        Check((await student.MyResults(default)) is OkObjectResult && (await student.MyLessons(default)) is OkObjectResult, "student history and current lessons queries translate");
        var teaching = new TeachingManagementController(db);
        teaching.ControllerContext = lecturer.ControllerContext;
        Check(await teaching.FinalizeAttendance("BH", default) is NoContentResult
            && !await db.PhienDiemDanhs.AnyAsync(x => x.MaBuoiHoc == "BH" && x.DongLucUtc == null), "finalizing lesson also closes QR sessions atomically");
        Check(Code(await lecturer.Open("BH", new(10), default)) == "AttendanceFinalized", "finalized lesson cannot reopen attendance");

        // One approved identity is reused across independent classes/lecturers.
        for (var n = 2; n <= 3; n++)
        {
            var teacherCode = n == 2 ? "GV2" : "GV";
            if (n == 2) db.GiangViens.Add(new() { MaGiangVien = teacherCode, HoTen = "Second lecturer" });
            db.BuoiHocs.Add(new() { MaBuoiHoc = $"BH{n}", NgayHocCuThe = now.Date, TrangThaiBuoiHoc = "ChuaDienRa",
                LichHoc = new() { MaLichHoc = $"LH{n}", MaPhong = "P",
                    LopHocPhan = new() { MaLopHocPhan = $"LOP{n}", MaHocKy = "HK", MaMonHoc = "MH", MaGiangVien = teacherCode } } });
            db.DanhSachSinhVienLops.Add(new() { MaSinhVien = "SV1", MaLopHocPhan = $"LOP{n}" });
        }
        await db.SaveChangesAsync();
        var globalProfile = await db.HoSoKhuonMats.AsNoTracking().SingleAsync(x => x.MaSinhVien == "SV1");
        for (var n = 2; n <= 3; n++)
        {
            var owner = Controller(n == 2 ? "GV2" : "GV", RoleNames.GiangVien);
            Check(await owner.ApprovalChallenge("SV1", default) is ForbidResult, $"class {n} lecturer uses officer approval without permission to approve again");
            var opened = Value<object>(await owner.Open($"BH{n}", new(10), default));
            var sid = (Guid)opened.GetType().GetProperty("sessionId")!.GetValue(opened)!;
            var classQr = Value<AttendanceQrResponse>(await owner.GetQr(sid, default));
            var classChallenge = Value<FaceChallengeResponse>(await student.Scan(new(classQr.QrToken), default));
            Check(await student.Confirm(new() { ChallengeId = classChallenge.ChallengeId, Frames = FreshFrames() }, default) is OkObjectResult, $"same approved face completes attendance in class {n}");
        }
        var after = await db.HoSoKhuonMats.AsNoTracking().SingleAsync(x => x.MaSinhVien == "SV1");
        Check(after.MaHoSo == globalProfile.MaHoSo && after.VectorJson == globalProfile.VectorJson
            && after.ApprovedAtUtc == globalProfile.ApprovedAtUtc && after.ApprovedBy == globalProfile.ApprovedBy
            && await db.HoSoKhuonMats.CountAsync(x => x.MaSinhVien == "SV1") == 1
            && await db.ChiTietDiemDanhs.CountAsync(x => x.MaSinhVien == "SV1" && (x.MaBuoiHoc == "BH2" || x.MaBuoiHoc == "BH3")) == 2,
            "two classes share exactly one unchanged profile and one approval");
        db.HoSoKhuonMats.Add(new() { MaHoSo = "FACE-SV2", MaSinhVien = "SV2", VectorJson = recognition.ProtectDescriptor(vector), ThoiGianCapNhat = DateTime.UtcNow,
            RegistrationPhotosProtected = photos.ProtectRegistration(RegistrationFrames()), ReviewVersion = Guid.NewGuid() });
        await db.SaveChangesAsync();
        await admin.SetIdentityPhoto("SV2", new() { Frame = Jpeg(70) }, default);
        var secondProfile = await db.HoSoKhuonMats.AsNoTracking().SingleAsync(x => x.MaSinhVien == "SV2");
        var secondReference = await db.StudentIdentityPhotos.AsNoTracking().SingleAsync(x => x.MaSinhVien == "SV2");
        Check(await admin.Approve("SV2", new() { ProfileVersion = secondProfile.ReviewVersion, IdentityPhotoVersion = secondReference.Version, ConfirmCompared = true }, default) is OkObjectResult
            && await db.HoSoKhuonMats.AnyAsync(x => x.MaSinhVien == "SV2" && x.ApprovedBy == "ADMIN" && x.ApprovedAtUtc != null), "Admin can approve and is recorded as the approving actor");
        Check(await admin.Revoke("SV2", default) is NoContentResult && await db.HoSoKhuonMats.AnyAsync(x => x.MaSinhVien == "SV1"), "Admin may revoke a specific global profile without affecting another student");
        var student2 = Controller("SV2", RoleNames.SinhVien);
        var firstRegistration = Value<FaceChallengeResponse>(await student2.EnrollmentChallenge(default));
        var callsBeforeConsent = fake.Calls;
        Check(Code(await student2.Enroll(new() { ChallengeId = firstRegistration.ChallengeId, Consent = true, Frames = RegistrationFrames() }, default)) == "ConsentRequired"
            && fake.Calls == callsBeforeConsent && await db.AttendanceChallenges.AnyAsync(x => x.Id == firstRegistration.ChallengeId && x.UsedAtUtc == null),
            "legacy client consent cannot silently enable newly retained registration photos");
        Check(await student2.Enroll(new() { ChallengeId = firstRegistration.ChallengeId, Consent = true, StoreReviewPhotosConsent = true, Frames = RegistrationFrames() }, default) is OkObjectResult, "new registration stores review images after live evidence succeeds");
        secondProfile = await db.HoSoKhuonMats.AsNoTracking().SingleAsync(x => x.MaSinhVien == "SV2");
        Check(secondProfile.RegistrationPhotosProtected != System.Text.Json.JsonSerializer.Serialize(RegistrationFrames())
            && photos.ReadRegistration(secondProfile.RegistrationPhotosProtected).Length == 3, "registration photos protected at rest and readable for authorized review");
        var returned = new FacePhotoReviewRequest { ProfileVersion = secondProfile.ReviewVersion, IdentityPhotoVersion = secondReference.Version, Note = "Ảnh đăng ký chưa khớp ảnh thẻ" };
        Check(Code(await admin.Reject("SV2", new() { ProfileVersion = secondProfile.ReviewVersion, IdentityPhotoVersion = secondReference.Version }, default)) == "RejectionReasonRequired", "returning a profile requires actionable reason");
        Check(await admin.Reject("SV2", returned, default) is OkObjectResult
            && await db.HoSoKhuonMats.AnyAsync(x => x.MaSinhVien == "SV2" && x.RejectedBy == "ADMIN" && x.RejectionReason == returned.Note), "photo rejection records actor and student-visible reason");
        var retryRegistration = Value<FaceChallengeResponse>(await student2.EnrollmentChallenge(default));
        Check(await student2.Enroll(new() { ChallengeId = retryRegistration.ChallengeId, Consent = true, StoreReviewPhotosConsent = true, Frames = [Jpeg(45), Jpeg(85), Jpeg(125)] }, default) is OkObjectResult,
            "student may resubmit only after staff returns profile");
        Check(Code(await admin.Approve("SV2", new() { ProfileVersion = returned.ProfileVersion, IdentityPhotoVersion = returned.IdentityPhotoVersion, ConfirmCompared = true }, default)) == "ReviewChanged",
            "old review cannot approve a resubmitted image set");
        // Exercise the actual downloaded ONNX graphs and native image decoding, independently of controller fakes.
        var repository = FindRoot();
        var modelPath = Path.Combine(repository, "backend", "QuanLySinhVien.Api", "Models", "Face");
        foreach (var pair in new[] { ("det_2.5g.onnx", 640), ("mobilenetv2_mcp.onnx", 112), ("MiniFASNetV2.onnx", 80) })
        {
            using var model = new InferenceSession(Path.Combine(modelPath, pair.Item1));
            var tensor = new DenseTensor<float>(new[] { 1, 3, pair.Item2, pair.Item2 });
            using var output = model.Run([NamedOnnxValue.CreateFromTensor(model.InputMetadata.Keys.First(), tensor)]);
            Check(output.All(x => x.AsTensor<float>().All(float.IsFinite)), "ONNX inference finite: " + pair.Item1);
            if (pair.Item1.StartsWith("det")) Check(output.Count == 9, "SCRFD output contract has nine heads");
            if (pair.Item1.StartsWith("mobilenet")) Check(output.First().AsTensor<float>().Length == 512, "MobileNetV2 outputs 512-dimensional embedding");
            if (pair.Item1.StartsWith("Mini")) Check(output.First().AsTensor<float>().Length == 3, "MiniFASNetV2 outputs three logits");
        }
        using var real = new FaceEvidenceService(new TestEnvironment { ContentRootPath = repository }, Options.Create(new FaceEvidenceOptions { ModelDirectory = modelPath }));
        string Blank(byte shade) { using var bitmap = new SKBitmap(400, 400); bitmap.Erase(new SKColor(shade, shade, shade)); using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80); return Convert.ToBase64String(data.ToArray()); }
        var blankPose = await real.InspectPoseAsync(Blank(0), default);
        Check(!blankPose.Usable && blankPose.Pose is null && blankPose.Code == "NoFaceDetected", "real SCRFD feedback rejects empty camera frame immediately");
        Check(!(await real.InspectPoseAsync("bad", default)).Usable, "live feedback rejects malformed image");
        try { await real.AnalyzeAsync([Blank(0), Blank(50), Blank(100)], "left", default); throw new Exception("Blank image accepted"); }
        catch (FaceEvidenceException e) { Check(e.Code == "NoFaceDetected", "real image decode and SCRFD reject blank camera evidence"); }
        try { await real.AnalyzeAsync(["bad", "other", "third"], "left", default); throw new Exception("Malformed image accepted"); }
        catch (FaceEvidenceException e) { Check(e.Code == "InvalidImage", "malformed image rejected without unhandled error"); }
        var samplePath = Path.Combine(repository, ".artifacts", "face-test.jpg");
        if (File.Exists(samplePath))
        {
            string FaceFrame(int quality) { using var image = SKImage.FromEncodedData(File.ReadAllBytes(samplePath)); using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality); return Convert.ToBase64String(data.ToArray()); }
            try { await real.AnalyzeAsync([FaceFrame(85), FaceFrame(88), FaceFrame(91)], "left", default); throw new Exception("Stationary test photo accepted"); }
            catch (FaceEvidenceException e) { Check(e.Code is "SpoofDetected" or "LivenessFailed", "real face photo reaches PAD/motion checks and is rejected"); }
        }
        Console.WriteLine($"{_count} attendance checks passed.");
    }
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend", "QuanLySinhVien.Api"))) directory = directory.Parent;
        return directory?.FullName ?? throw new Exception("Run from repository root");
    }
    private sealed class Evidence(double[] vector) : IFaceEvidenceService
    {
        public Task<FacePoseObservation> InspectPoseAsync(string frame, CancellationToken ct) => Task.FromResult(new FacePoseObservation(true, "FaceReady", "OK", 0, .12));
        public double[] Vector = vector; public int Calls; public Func<Task>? BeforeReturn; public string? FailCode;
        public async Task<IReadOnlyCollection<double[]>> AnalyzeAsync(string[] frames, string direction, CancellationToken ct)
        { Calls++; if (BeforeReturn != null) await BeforeReturn(); if (FailCode != null) throw new FaceEvidenceException(FailCode, "test"); return [Vector, Vector, Vector]; }
    }
    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test"; public string ApplicationName { get; set; } = "Checks";
        public string ContentRootPath { get; set; } = ""; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}



