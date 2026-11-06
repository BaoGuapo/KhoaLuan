using QuanLySinhVien.Api.Contracts.Academic;

namespace QuanLySinhVien.Api.Services;

/// <summary>Một môn của sinh viên trong một học kỳ: điểm đã công bố (nếu có) và số buổi điểm danh.</summary>
public sealed record TranscriptCourse(
    string MaMonHoc,
    string TenMonHoc,
    string MaHocKy,
    string? MaLopHocPhan,
    int SoTinChi,
    string? DiemChu,
    AttendanceTallyResponse? ChuyenCan);

public sealed record TranscriptSemester(string MaHocKy, string TenHocKy, DateTime? NgayBatDau);

/// <summary>
/// Tổng hợp bảng điểm theo học kỳ. GPA học kỳ tính trên các môn có điểm trong kỳ; GPA tích lũy lấy điểm
/// cao nhất của mỗi môn qua các lần học; tín chỉ tích lũy chỉ tính môn đạt. Môn 0 tín chỉ không vào GPA.
/// </summary>
public static class AcademicTranscript
{
    public static decimal? Diem4(string? diemChu) => diemChu switch
    {
        "A" => 4m, "B+" => 3.5m, "B" => 3m, "C+" => 2.5m, "C" => 2m, "D+" => 1.5m, "D" => 1m, "F" => 0m,
        _ => null
    };

    public static AcademicResultResponse Build(
        IEnumerable<TranscriptSemester> semesters,
        IEnumerable<TranscriptCourse> courses)
    {
        var bySemester = courses.GroupBy(x => x.MaHocKy).ToDictionary(x => x.Key, x => x.ToList());
        var ordered = semesters
            .Where(x => bySemester.ContainsKey(x.MaHocKy))
            .OrderBy(x => x.NgayBatDau ?? DateTime.MaxValue)
            .ThenBy(x => x.MaHocKy, StringComparer.Ordinal)
            .ToList();

        // Điểm cao nhất của từng môn tính đến học kỳ đang xét.
        var best = new Dictionary<string, (decimal Diem, int TinChi)>(StringComparer.OrdinalIgnoreCase);
        var results = new List<SemesterResultResponse>();
        foreach (var semester in ordered)
        {
            var rows = bySemester[semester.MaHocKy];
            var graded = rows.Where(x => x.SoTinChi > 0 && Diem4(x.DiemChu) is not null).ToList();
            foreach (var row in graded)
            {
                var diem = Diem4(row.DiemChu)!.Value;
                if (!best.TryGetValue(row.MaMonHoc, out var current) || diem > current.Diem)
                    best[row.MaMonHoc] = (diem, row.SoTinChi);
            }

            results.Add(new SemesterResultResponse(
                semester.MaHocKy,
                semester.TenHocKy,
                semester.NgayBatDau,
                Average(graded.Select(x => (Diem4(x.DiemChu)!.Value, x.SoTinChi))),
                graded.Where(x => x.DiemChu != "F").Sum(x => x.SoTinChi),
                Average(best.Values),
                best.Values.Where(x => x.Diem > 0).Sum(x => x.TinChi),
                rows.OrderBy(x => x.MaMonHoc, StringComparer.Ordinal).Select(ToCourse).ToList()));
        }

        var pending = bySemester.Values.SelectMany(x => x)
            .Where(x => Diem4(x.DiemChu) is null && !best.ContainsKey(x.MaMonHoc))
            .Sum(x => x.SoTinChi);
        results.Reverse();
        return new AcademicResultResponse(
            Average(best.Values),
            best.Values.Where(x => x.Diem > 0).Sum(x => x.TinChi),
            pending,
            results);
    }

    private static CourseResultResponse ToCourse(TranscriptCourse row)
    {
        var diem = Diem4(row.DiemChu);
        var status = diem is null ? "ChuaCoDiem" : diem > 0 ? "Dat" : "KhongDat";
        return new CourseResultResponse(row.MaMonHoc, row.TenMonHoc, row.MaLopHocPhan, row.SoTinChi,
            diem is null ? null : row.DiemChu, diem, status, row.ChuyenCan);
    }

    private static decimal? Average(IEnumerable<(decimal Diem, int TinChi)> values)
    {
        var list = values.Where(x => x.TinChi > 0).ToList();
        var credits = list.Sum(x => x.TinChi);
        return credits == 0 ? null : Math.Round(list.Sum(x => x.Diem * x.TinChi) / credits, 2);
    }
}
