using QuanLySinhVien.Api.Services;

internal static class AcademicTranscriptChecks
{
    private static int _count;

    private static void Check(bool success, string label)
    { if (!success) throw new Exception(label); _count++; Console.WriteLine("PASS: " + label); }

    public static void Run()
    {
        Check(AcademicTranscript.Diem4("A") == 4m && AcademicTranscript.Diem4("B+") == 3.5m
            && AcademicTranscript.Diem4("D") == 1m && AcademicTranscript.Diem4("F") == 0m, "quy đổi điểm chữ sang thang 4");
        Check(AcademicTranscript.Diem4("") is null && AcademicTranscript.Diem4(null) is null, "chưa có điểm thì không quy đổi");

        var semesters = new[]
        {
            new TranscriptSemester("HK2", "HK2", new DateTime(2026, 2, 1)),
            new TranscriptSemester("HK1", "HK1", new DateTime(2025, 9, 1)),
            new TranscriptSemester("HK3", "HK3", new DateTime(2026, 9, 1)),
            new TranscriptSemester("TRONG", "Không có môn", new DateTime(2027, 2, 1))
        };
        var result = AcademicTranscript.Build(semesters, new[]
        {
            Course("CSDL", "HK1", 3, "F"),
            Course("WEB", "HK1", 2, "A"),
            Course("SHCD", "HK1", 0, "A"),
            Course("CSDL", "HK2", 3, "B"),
            Course("AI", "HK2", 3, "C"),
            Course("IOT", "HK3", 3, null)
        });

        Check(result.HocKy.Select(x => x.MaHocKy).SequenceEqual(new[] { "HK3", "HK2", "HK1" }),
            "học kỳ mới nhất đứng đầu, bỏ học kỳ không có môn");
        var hk1 = result.HocKy.Single(x => x.MaHocKy == "HK1");
        // (0×3 + 4×2) / 5 = 1,6; môn 0 tín chỉ không vào GPA.
        Check(hk1.GpaHocKy == 1.6m, "GPA học kỳ tính cả môn F, bỏ môn 0 tín chỉ");
        Check(hk1.TinChiDat == 2 && hk1.TinChiTichLuy == 2, "môn F không tính tín chỉ đạt");
        var hk2 = result.HocKy.Single(x => x.MaHocKy == "HK2");
        // CSDL học lại lấy B thay F: (3×3 + 4×2 + 2×3) / 8 = 2,875.
        Check(hk2.GpaTichLuy == 2.88m && hk2.TinChiTichLuy == 8, "GPA tích lũy lấy điểm cao nhất khi học lại");
        var hk3 = result.HocKy.Single(x => x.MaHocKy == "HK3");
        Check(hk3.GpaHocKy is null && hk3.GpaTichLuy == 2.88m, "học kỳ chưa có điểm giữ nguyên GPA tích lũy");
        Check(hk3.MonHoc.Single().TrangThai == "ChuaCoDiem" && hk3.MonHoc.Single().DiemChu is null, "môn chưa công bố hiện chưa có điểm");
        Check(result.GpaTichLuy == 2.88m && result.TinChiTichLuy == 8 && result.TinChiChuaCoDiem == 3,
            "tổng hợp toàn khóa");
        Check(hk1.MonHoc.Single(x => x.MaMonHoc == "CSDL").TrangThai == "KhongDat", "điểm F là không đạt");

        Console.WriteLine($"{_count} transcript checks passed.");
    }

    private static TranscriptCourse Course(string code, string semester, int credits, string? grade) =>
        new(code, code, semester, null, credits, grade, null);
}
