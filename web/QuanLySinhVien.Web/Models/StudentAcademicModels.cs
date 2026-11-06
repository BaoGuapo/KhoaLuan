namespace QuanLySinhVien.Web.Models;

public sealed class StudentScheduleSlot
{
    public int ThuTrongTuan { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public string TenPhong { get; set; } = "";
    public string Day => ThuTrongTuan == 8 ? "Chủ nhật" : $"Thứ {ThuTrongTuan}";
}
public sealed class StudentCourseClass
{
    public string LoaiDangKy { get; set; } = "moi";
    public string? LyDoKhongDuDieuKien { get; set; }
    public string? DiemDaCo { get; set; }
    public string MaLopHocPhan { get; set; } = "";
    public string MaMonHoc { get; set; } = "";
    public string TenMonHoc { get; set; } = "";
    public string MaHocKy { get; set; } = "";
    public string HoTen { get; set; } = "";
    public int SoTinChiLyThuyet { get; set; }
    public int SoTinChiThucHanh { get; set; }
    public int SiSoToiDa { get; set; }
    public int SiSoHienTai { get; set; }
    public bool DaDangKy { get; set; }
    public DateTime? ThoiGianDangKy { get; set; }
    public List<StudentScheduleSlot> LichHoc { get; set; } = [];
    public int Credits => SoTinChiLyThuyet + SoTinChiThucHanh;
    public int Seats => Math.Max(0, SiSoToiDa - SiSoHienTai);
}
public sealed class StudentSemester
{
    public string MaHocKy { get; set; } = "";
    public string TenHocKy { get; set; } = "";
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
}
public sealed class StudentAcademicWorkspace
{
    public string RegistrationType { get; set; } = "moi";
    public string? CurrentSemester { get; set; }
    public bool RegistrationOpen { get; set; }
    public int? MinCredits { get; set; }
    public int? MaxCredits { get; set; }
    public decimal? Gpa { get; set; }
    public List<StudentRegistrationWindow> Windows { get; set; } = [];
    public static string TypeLabel(string type) => type switch { "lai" => "Học lại", "caithien" => "Học cải thiện", "vuot" => "Học vượt", _ => "Học mới" };
    public List<StudentCourseClass> Available { get; set; } = [];
    public List<StudentCourseClass> Registered { get; set; } = [];
    public List<StudentSemester> Semesters { get; set; } = [];
    public string? Error { get; set; }
    public string? Semester { get; set; }
    public string? Query { get; set; }
    public string? Subject { get; set; }
    public bool OnlyAvailable { get; set; }
    public DateTime WeekStart { get; set; }
    public IEnumerable<StudentCourseClass> SemesterRegistered => Registered.Where(x => x.MaHocKy == Semester);
    public IEnumerable<StudentCourseClass> Filtered => Available.Where(x => x.MaHocKy == Semester)
        .Where(x => string.IsNullOrWhiteSpace(Query) || x.MaMonHoc.Contains(Query, StringComparison.OrdinalIgnoreCase) || x.TenMonHoc.Contains(Query, StringComparison.OrdinalIgnoreCase))
        .Where(x => !OnlyAvailable || x.Seats > 0 || x.DaDangKy);
    public List<IGrouping<string, StudentCourseClass>> Subjects => Filtered.GroupBy(x => x.MaMonHoc).OrderBy(x => x.First().TenMonHoc).ToList();
    public IGrouping<string, StudentCourseClass>? Selected => Subjects.FirstOrDefault(x => x.Key == Subject) ?? Subjects.FirstOrDefault();
    public bool HasSubject(string subject) => SemesterRegistered.Any(x => x.MaMonHoc == subject);
    public bool Conflicts(StudentCourseClass candidate) => candidate.LichHoc.Any(slot => SemesterRegistered.Any(enrolled =>
        enrolled.MaLopHocPhan != candidate.MaLopHocPhan && enrolled.LichHoc.Any(existing => existing.ThuTrongTuan == slot.ThuTrongTuan &&
            existing.TietBatDau <= slot.TietKetThuc && slot.TietBatDau <= existing.TietKetThuc)));
}
public sealed class StudentRegistrationWindow
{
    public string TenDot { get; set; } = "";
    public DateTime DongLucUtc { get; set; }
    public string ClosingTime => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.SpecifyKind(DongLucUtc, DateTimeKind.Utc), "SE Asia Standard Time").ToString("HH:mm dd/MM/yyyy");
}

/// <summary>Trang chủ sinh viên: hồ sơ, lớp học phần học kỳ hiện tại và lịch hôm nay/tuần này.</summary>
public sealed class StudentDashboardViewModel
{
    public UserDetailModel? HoSo { get; init; }
    public StudentAcademicWorkspace? HocTap { get; init; }
    public DateTime HomNay { get; init; }

    /// <summary>Thứ theo cách trường dùng: 2 là thứ Hai, 8 là Chủ nhật.</summary>
    public int ThuHomNay => HomNay.DayOfWeek == DayOfWeek.Sunday ? 8 : (int)HomNay.DayOfWeek + 1;
    public IReadOnlyList<StudentCourseClass> LopHocKy => HocTap?.SemesterRegistered.ToList() ?? [];
    public int SoBuoiTrongTuan => LopHocKy.Sum(x => x.LichHoc.Count);
    public int TinChiHocKy => LopHocKy.Sum(x => x.Credits);
    public IReadOnlyList<(StudentCourseClass Lop, StudentScheduleSlot Buoi)> LichHomNay =>
        LopHocKy.SelectMany(x => x.LichHoc.Where(s => s.ThuTrongTuan == ThuHomNay).Select(s => (x, s)))
            .OrderBy(x => x.s.TietBatDau).ToList();
    public string? TenHocKy => HocTap?.Semesters.FirstOrDefault(x => x.MaHocKy == HocTap.Semester)?.TenHocKy;
}
