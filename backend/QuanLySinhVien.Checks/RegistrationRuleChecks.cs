using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

public static class RegistrationRuleChecks
{
    public static void Run()
    {
        var count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); count++; Console.WriteLine("PASS: " + message); }
        var now = new DateTime(2026, 10, 19, 2, 0, 0, DateTimeKind.Utc);
        var profile = new HoSoHocVu { MaSinhVien = "S", ThuTuHocKy = 3, GpaTichLuy = 2.5m };
        var period = new DotDangKyHocPhan { MaHocKy = "CURRENT", MoLucUtc = now.AddDays(-1), DongLucUtc = now.AddDays(1) };
        var previous = new HocKyThucTe { MaHocKy = "OLD", NgayBatDau = new(2026,1,1), NgayKetThuc = new(2026,6,1) };
        var course = new LopHocPhan { MaLopHocPhan = "CLASS", MaHocKy = "CURRENT", MaMonHoc = "NEW", MonHoc = new() { SoTinChiLyThuyet = 3 } };
        var rules = new StudentRegistrationRules { NowUtc = now, Profile = profile, Windows = [period],
            Curriculum = [new() { MaMonHoc = "NEW", ThuTuHocKy = 3 }, new() { MaMonHoc = "ADV", ThuTuHocKy = 5 }] };
        Check(rules.CurrentSemester == "CURRENT", "open registration semester selected even without class offers");
        Check(rules.IsOpen(period) && !rules.IsOpen(new() { MoLucUtc = now.AddDays(-1), DongLucUtc = now }), "registration deadline is exclusive");
        Check(rules.Block(course, "moi") is null, "new course registration allowed during open round");
        Check(rules.Classify("ADV", "CURRENT") == "vuot", "advanced courses keep the current offering semester");
        Check(rules.Block(course, "caithien") is not null && rules.Block(course, "invalid") is not null, "reject forged registration type without passed results");
        var result = new KetQuaHocPhan { MaMonHoc = "NEW", MaHocKy = "OLD", DiemChu = "F", DaCongBo = true, HocKy = previous };
        rules.Results.Add(result);
        Check(rules.Classify("NEW", "CURRENT") == "lai", "published failing grade classified as retake");
        result.DiemChu = "D";
        Check(rules.Classify("NEW", "CURRENT") == "caithien", "a passed D grade is eligible for improvement");
        result.DiemChu = "A";
        Check(rules.Classify("NEW", "CURRENT") == "caithien", "improvement is not restricted to low passing grades");
        result.DaCongBo = false;
        Check(rules.Classify("NEW", "CURRENT") == "moi", "unpublished marks never establish improvement eligibility");
        rules.Results.Clear();
        rules.Requirements.Add(new() { MaMonHoc = "NEW", MaMonDieuKien = "PRE", Loai = "tienquyet" });
        Check(rules.Block(course, "moi") is not null, "missing prerequisite prevents enrollment");
        rules.Results.Add(new() { MaMonHoc = "PRE", MaHocKy = "OLD", DiemChu = "F", DaCongBo = true, HocKy = previous });
        Check(rules.Block(course, "moi") is not null, "failed prerequisite cannot unlock advanced course");
        rules.Requirements[0].Loai = "hoctruoc";
        Check(rules.Block(course, "moi") is null, "previous-course condition accepts a finalized failing grade");
        rules.Requirements[0].Loai = "songhanh";
        rules.Enrollments.Add(new() { LopHocPhan = new() { MaHocKy = "CURRENT", MaMonHoc = "PRE", MonHoc = new() { SoTinChiLyThuyet = 12 } } });
        Check(rules.Block(course, "moi") is null, "concurrent course may already be enrolled in the same semester");
        profile.GpaTichLuy = 1.99m;
        Check(rules.MaxCredits("CURRENT") == 14 && rules.Block(course, "moi") is not null, "GPA below 2 enforces 14-credit maximum in the transaction rules");
        period.LaHocKyHe = true;
        Check(rules.MaxCredits("CURRENT") is null && rules.MinCredits("CURRENT") is null, "summer has no minimum or maximum credit load");
        period.LaHocKyHe = false; profile.GpaTichLuy = 2m;
        Check(rules.MaxCredits("CURRENT") is null && rules.MinCredits("CURRENT") == 14, "GPA of exactly 2 uses regular main-semester load");
        profile.LaHocKyDau = true;
        Check(rules.Block(course, "moi") is not null, "first-semester students do not self-register");
        Check(rules.Block(course, "moi", selfRegistration: false) is null, "academic staff may place first-semester students into classes");
        profile.LaHocKyDau = false; period.ChoPhepDangKy = false;
        Check(rules.Block(course, "moi") is not null, "closed round cannot be bypassed by POST");
        var emptyCurrent = new StudentRegistrationRules { NowUtc = now, Semesters = [previous, new() { MaHocKy = "CURRENT", NgayBatDau = new(2026,9,1), NgayKetThuc = new(2026,12,31) }] };
        Check(emptyCurrent.CurrentSemester == "CURRENT", "empty current semester does not fall back to historical data");
        var webDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time").Date;
        var webWorkspace = new QuanLySinhVien.Web.Models.StudentAcademicWorkspace { Semesters = [new() { MaHocKy = "CURRENT", NgayBatDau = webDate.AddDays(-2), NgayKetThuc = webDate.AddDays(2) }, new() { MaHocKy = "OLD", NgayBatDau = new(2000,1,1), NgayKetThuc = new(2000,6,1) }], Registered = [new() { MaHocKy = "OLD" }] };
        QuanLySinhVien.Web.Controllers.SinhVienController.SelectSemester(webWorkspace, null);
        Check(webWorkspace.Semester == "CURRENT", "schedule UI also defaults to current semester even when only past enrollments exist");
        Console.WriteLine($"{count} registration rule checks passed.");
    }
}
