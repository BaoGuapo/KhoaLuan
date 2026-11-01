using QuanLySinhVien.Api.Contracts.Training;

namespace QuanLySinhVien.Api.Services;

public static class TrainingRules
{
    public static string? ValidateCourses(IReadOnlyCollection<CurriculumCourseRequest> courses)
    {
        if (courses.Count is < 1 or > 200) return "Chương trình cần từ 1 đến 200 môn.";
        if (courses.Any(x => x is null)) return "Dòng môn học không được để rỗng.";
        if (courses.Any(x => string.IsNullOrWhiteSpace(x.MaMonHoc) || x.MaMonHoc.Trim().Length > 50 || x.ThuTuHocKy is < 1 or > 12))
            return "Mã môn hoặc học kỳ dự kiến không hợp lệ.";
        if (courses.Select(x => x.MaMonHoc.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != courses.Count)
            return "Một môn chỉ được xuất hiện một lần trong chương trình.";
        return null;
    }

    public static string[] ClassCodes(string prefix, int count)
    {
        if (count is < 1 or > 50 || string.IsNullOrWhiteSpace(prefix) || prefix.Length > 24 ||
            !System.Text.RegularExpressions.Regex.IsMatch(prefix, @"^[A-Za-z0-9_-]+$"))
            throw new ArgumentException("Tiền tố hoặc số lớp không hợp lệ.");
        return Enumerable.Range(1, count).Select(x => $"{prefix}-{x:00}").ToArray();
    }

    public static IEnumerable<DateTime> SessionDates(DateTime start, DateTime end, int weekday)
    {
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            if ((date.DayOfWeek == DayOfWeek.Sunday ? 8 : (int)date.DayOfWeek + 1) == weekday)
                yield return date;
    }
}
