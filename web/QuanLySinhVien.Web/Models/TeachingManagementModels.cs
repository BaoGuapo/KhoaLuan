using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Web.Models;

public sealed class EligibleCourseModel
{
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int SoTinChiLyThuyet { get; set; }
    public int SoTinChiThucHanh { get; set; }
    public string MaNganh { get; set; } = string.Empty;
    public string TenNganh { get; set; } = string.Empty;
}

public sealed class AcademicOptionModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class TeachingAssignmentModel
{
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string MaHocKy { get; set; } = string.Empty;
    public string TenHocKy { get; set; } = string.Empty;
    public string MaGiangVien { get; set; } = string.Empty;
    public string HoTenGiangVien { get; set; } = string.Empty;
    public int SiSoHienTai { get; set; }
    public int SiSoToiDa { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string MaLichHoc { get; set; } = string.Empty;
    public int ThuTrongTuan { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public string MaPhong { get; set; } = string.Empty;
    public string TenPhong { get; set; } = string.Empty;
}

public sealed class LecturerAssignmentWorkspaceModel
{
    public string MaGiangVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? Khoa { get; set; }
    public string? ChuyenNganh { get; set; }
    public IReadOnlyCollection<EligibleCourseModel> MonHocPhuHop { get; set; } = [];
    public IReadOnlyCollection<AcademicOptionModel> HocKy { get; set; } = [];
    public IReadOnlyCollection<AcademicOptionModel> PhongHoc { get; set; } = [];
    public IReadOnlyCollection<TeachingAssignmentModel> PhanCongHienTai { get; set; } = [];
}

public sealed class CreateTeachingAssignmentViewModel
{
    public string MaGiangVien { get; set; } = string.Empty;
    [Required, MaxLength(30), Display(Name = "Mã lớp học phần")] public string MaLopHocPhan { get; set; } = string.Empty;
    [Required, Display(Name = "Môn học")] public string MaMonHoc { get; set; } = string.Empty;
    [Required, Display(Name = "Học kỳ")] public string MaHocKy { get; set; } = string.Empty;
    [Required, Display(Name = "Phòng học")] public string MaPhong { get; set; } = string.Empty;
    [Range(1, 500), Display(Name = "Sĩ số tối đa")] public int SiSoToiDa { get; set; } = 70;
    [Range(2, 8), Display(Name = "Thứ trong tuần")] public int ThuTrongTuan { get; set; } = 2;
    [Range(1, 20), Display(Name = "Tiết bắt đầu")] public int TietBatDau { get; set; } = 1;
    [Range(1, 20), Display(Name = "Tiết kết thúc")] public int TietKetThuc { get; set; } = 3;
}

public sealed class TeachingAssignmentPageViewModel
{
    public required LecturerAssignmentWorkspaceModel Workspace { get; init; }
    public required CreateTeachingAssignmentViewModel Form { get; init; }
}

public sealed class AttendanceSessionModel
{
    public string MaBuoiHoc { get; set; } = string.Empty;
    public DateTime NgayHocCuThe { get; set; }
    public string TrangThaiBuoiHoc { get; set; } = string.Empty;
    public bool DaChot { get; set; }
}

public sealed class StudentAttendanceModel
{
    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string TrangThaiBuoiHoc { get; set; } = string.Empty;
    public DateTime? ThoiGianCheckIn { get; set; }
    public int CoMat { get; set; }
    public int DiTre { get; set; }
    public int CoPhep { get; set; }
    public int Vang { get; set; }
    public int TongSoBuoi { get; set; }
    public string? TrangThaiCanhBao { get; set; }
}

public sealed class LecturerClassWorkspaceModel
{
    public TeachingAssignmentModel LopHocPhan { get; set; } = new();
    public IReadOnlyCollection<AttendanceSessionModel> BuoiHoc { get; set; } = [];
    public string? MaBuoiHocDangChon { get; set; }
    public IReadOnlyCollection<StudentAttendanceModel> SinhVien { get; set; } = [];
    public bool CoTheDiemDanh { get; set; }
    public string? LyDoKhongDiemDanh { get; set; }
    public string KhungGioTiet { get; set; } = string.Empty;
    public DateTime? MoDiemDanhLuc { get; set; }
    public DateTime? DongDiemDanhLuc { get; set; }
}

public sealed class AttendanceSessionStatisticsModel
{
    public string MaBuoiHoc { get; set; } = string.Empty;
    public DateTime NgayHocCuThe { get; set; }
    public string TrangThaiBuoiHoc { get; set; } = string.Empty;
    public bool DaChot { get; set; }
    public bool LaHomNay { get; set; }
    public int SiSoLop { get; set; }
    public int CoMat { get; set; }
    public int DiTre { get; set; }
    public int CoPhep { get; set; }
    public int Vang { get; set; }
    public int ChuaDiemDanh { get; set; }

    public int DaDiemDanh => CoMat + DiTre + CoPhep + Vang;
    public double TyLeChuyenCan => SiSoLop == 0 ? 0 : (double)(CoMat + DiTre) / SiSoLop;
}

public sealed class ClassAttendanceHistoryModel
{
    public TeachingAssignmentModel LopHocPhan { get; set; } = new();
    public string KhungGioTiet { get; set; } = string.Empty;
    public IReadOnlyCollection<AttendanceSessionStatisticsModel> BuoiHoc { get; set; } = [];
}

/// <summary>Trang lịch sử: bảng thống kê mọi buổi, kèm chi tiết buổi đang mở (nếu có).</summary>
public sealed class ClassAttendanceHistoryPageViewModel
{
    public required ClassAttendanceHistoryModel LichSu { get; init; }
    public LecturerClassWorkspaceModel? ChiTietBuoi { get; init; }
}
