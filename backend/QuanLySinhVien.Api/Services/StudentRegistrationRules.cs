using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Services;

public sealed class StudentRegistrationRules
{
    public const string SourceUrl = "https://doc.huit.edu.vn/images/documents/n00ct/2025-qcdt-dai-hoc-05092025-final-20250909042553-e.pdf";
    public static readonly string[] Types = ["moi", "lai", "caithien", "vuot"];
    public static readonly string[] Grades = ["A", "B+", "B", "C+", "C", "D+", "D", "F"];
    public HoSoHocVu? Profile { get; init; }
    public List<KetQuaHocPhan> Results { get; init; } = [];
    public List<MonKeHoachDaoTao> Curriculum { get; init; } = [];
    public List<DieuKienHocPhan> Requirements { get; init; } = [];
    public List<DotDangKyHocPhan> Windows { get; init; } = [];
    public List<DanhSachSinhVienLop> Enrollments { get; init; } = [];
    public List<HocKyThucTe> Semesters { get; init; } = [];
    public DateTime NowUtc { get; init; } = DateTime.UtcNow;
    public DateTime Today => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(NowUtc, "SE Asia Standard Time").Date;
    public string? CurrentSemester => Windows.Where(IsOpen).OrderByDescending(x => x.MoLucUtc).ThenBy(x => x.MaHocKy).FirstOrDefault()?.MaHocKy
        ?? Semesters.Where(x => x.NgayBatDau <= Today && x.NgayKetThuc >= Today).OrderByDescending(x => x.NgayBatDau).ThenBy(x => x.MaHocKy).FirstOrDefault()?.MaHocKy;
    public bool IsOpen(DotDangKyHocPhan window) => window.ChoPhepDangKy && window.MoLucUtc <= NowUtc && NowUtc < window.DongLucUtc;
    public int? MaxCredits(string semester) => Windows.Any(x => x.MaHocKy == semester && IsOpen(x) && x.LaHocKyHe) ? null
        : Profile?.GpaTichLuy is < 2m ? 14 : null;
    public int? MinCredits(string semester) => Windows.Any(x => x.MaHocKy == semester && IsOpen(x) && x.LaHocKyHe) || Profile?.LaHocKyCuoi == true ? null
        : Profile?.GpaTichLuy is < 2m ? 10 : Profile?.GpaTichLuy is >= 2m ? 14 : null;
    private IEnumerable<KetQuaHocPhan> Previous(string course, string semester) => Results.Where(x => x.MaMonHoc == course && x.MaHocKy != semester && x.DaCongBo && Grades.Contains(x.DiemChu)
        && x.HocKy.NgayKetThuc < Today);
    public bool Passed(string course, string semester) => Previous(course, semester).Any(x => x.DiemChu != "F");
    public string? PreviousGrade(string course, string semester) => Previous(course, semester).OrderBy(x => Array.IndexOf(Grades, x.DiemChu)).FirstOrDefault()?.DiemChu;
    public string Classify(string course, string semester)
    {
        var previous = Previous(course, semester).ToList();
        if (previous.Any(x => x.DiemChu != "F")) return "caithien";
        if (previous.Any(x => x.DiemChu == "F")) return "lai";
        return Profile is not null && Curriculum.Any(x => x.MaMonHoc == course && x.ThuTuHocKy > Profile.ThuTuHocKy) ? "vuot" : "moi";
    }
    public string? Block(LopHocPhan course, string type, bool selfRegistration = true)
    {
        if (!Types.Contains(type)) return "Loại đăng ký không hợp lệ.";
        if (!Windows.Any(x => x.MaHocKy == course.MaHocKy && IsOpen(x))) return "Học kỳ này không có đợt đăng ký đang mở cho bạn.";
        if (Profile is null || Profile.ThuTuHocKy < 1) return "Chưa có hồ sơ học vụ để kiểm tra điều kiện đăng ký. Vui lòng liên hệ phòng đào tạo.";
        if (Profile.LaHocKyDau && selfRegistration) return "Tân sinh viên được nhà trường xếp học phần học kỳ đầu, không tự đăng ký.";
        if (!Curriculum.Any(x => x.MaMonHoc == course.MaMonHoc)) return "Môn học chưa thuộc chương trình đào tạo đã xác nhận của bạn.";
        if (Classify(course.MaMonHoc, course.MaHocKy) != type) return "Loại đăng ký không phù hợp kết quả học tập hoặc tiến độ chương trình của bạn.";
        if (Enrollments.Any(x => x.LopHocPhan.MaHocKy == course.MaHocKy && x.LopHocPhan.MaMonHoc == course.MaMonHoc)) return "Bạn đã đăng ký môn này trong học kỳ.";
        // A finalized result also prevents a second attempt within the same semester.
        if (Results.Any(x => x.MaHocKy == course.MaHocKy && x.MaMonHoc == course.MaMonHoc)) return "Môn này đã có kết quả trong học kỳ, không được đăng ký lần hai.";
        if (Profile.GpaTichLuy is null && !(Profile.LaHocKyDau && !selfRegistration) && !Windows.Any(x => x.MaHocKy == course.MaHocKy && IsOpen(x) && x.LaHocKyHe)) return "Chưa có GPA tích lũy được xác nhận để kiểm tra giới hạn tín chỉ.";
        var credits = Enrollments.Where(x => x.LopHocPhan.MaHocKy == course.MaHocKy).Sum(x => x.LopHocPhan.MonHoc.SoTinChiLyThuyet + x.LopHocPhan.MonHoc.SoTinChiThucHanh);
        if (MaxCredits(course.MaHocKy) is int max && credits + course.MonHoc.SoTinChiLyThuyet + course.MonHoc.SoTinChiThucHanh > max) return $"Tổng đăng ký vượt giới hạn {max} tín chỉ của học kỳ.";
        foreach (var requirement in Requirements.Where(x => x.MaMonHoc == course.MaMonHoc))
        {
            var passed = Passed(requirement.MaMonDieuKien, course.MaHocKy);
            var allowed = requirement.Loai switch
            {
                "tienquyet" => passed,
                "hoctruoc" => Previous(requirement.MaMonDieuKien, course.MaHocKy).Any(),
                "songhanh" => passed || Enrollments.Any(x => x.LopHocPhan.MaHocKy == course.MaHocKy && x.LopHocPhan.MaMonHoc == requirement.MaMonDieuKien),
                _ => false
            };
            if (!allowed) return $"Chưa đáp ứng học phần {requirement.MaMonDieuKien} ({requirement.Loai switch { "tienquyet" => "tiên quyết", "hoctruoc" => "học trước", _ => "song hành" }}).";
        }
        return null;
    }
    public static async Task<StudentRegistrationRules> LoadAsync(AppDbContext db, string studentCode, CancellationToken ct)
    {
        var student = await db.SinhViens.AsNoTracking().SingleAsync(x => x.MaSinhVien == studentCode, ct);
        var major = student.Nganh?.Trim() ?? ""; var intake = student.KhoaHoc?.Trim() ?? "";
        var majors = await db.Database.SqlQuery<string>($"SELECT maNganh AS Value FROM NganhHoc WHERE maNganh={major} OR tenNganh={major}").ToListAsync(ct);
        var intakes = await db.Database.SqlQuery<string>($"SELECT maKhoa AS Value FROM KhoaTuyenSinh WHERE maKhoa={intake} OR CONVERT(nvarchar(10),namBatDau)={intake}").ToListAsync(ct);
        var plans = await db.KeHoachDaoTaos.Where(x => majors.Contains(x.MaNganh) && intakes.Contains(x.MaKhoa)).Select(x => x.Id).ToListAsync(ct);
        return new()
        {
            Profile = await db.HoSoHocVus.AsNoTracking().SingleOrDefaultAsync(x => x.MaSinhVien == studentCode, ct),
            Results = await db.KetQuaHocPhans.AsNoTracking().Include(x => x.HocKy).Where(x => x.MaSinhVien == studentCode).ToListAsync(ct),
            Curriculum = await db.MonKeHoachDaoTaos.AsNoTracking().Where(x => plans.Contains(x.KeHoachDaoTaoId)).ToListAsync(ct),
            Requirements = await db.DieuKienHocPhans.AsNoTracking().Where(x => plans.Contains(x.KeHoachDaoTaoId)).ToListAsync(ct),
            Windows = await db.DotDangKyHocPhans.AsNoTracking().Where(x => x.KeHoachDaoTaoId == null || plans.Contains(x.KeHoachDaoTaoId.Value)).ToListAsync(ct),
            Enrollments = await db.DanhSachSinhVienLops.AsNoTracking().Include(x => x.LopHocPhan).ThenInclude(x => x.MonHoc).Where(x => x.MaSinhVien == studentCode).ToListAsync(ct),
            Semesters = await db.HocKys.AsNoTracking().ToListAsync(ct)
        };
    }
}
