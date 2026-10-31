using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Teaching;

public sealed record EligibleCourseResponse(
    string MaMonHoc,
    string TenMonHoc,
    int SoTinChiLyThuyet,
    int SoTinChiThucHanh,
    string MaNganh,
    string TenNganh);

public sealed record TeachingAssignmentResponse(
    string MaLopHocPhan,
    string MaMonHoc,
    string TenMonHoc,
    string MaHocKy,
    string TenHocKy,
    string MaGiangVien,
    string HoTenGiangVien,
    int SiSoHienTai,
    int SiSoToiDa,
    string TrangThai,
    string MaLichHoc,
    int ThuTrongTuan,
    int TietBatDau,
    int TietKetThuc,
    string MaPhong,
    string TenPhong);

public sealed record AcademicOptionResponse(string Code, string Name);

public sealed record LecturerAssignmentWorkspaceResponse(
    string MaGiangVien,
    string HoTen,
    string? Khoa,
    string? ChuyenNganh,
    IReadOnlyCollection<EligibleCourseResponse> MonHocPhuHop,
    IReadOnlyCollection<AcademicOptionResponse> HocKy,
    IReadOnlyCollection<AcademicOptionResponse> PhongHoc,
    IReadOnlyCollection<TeachingAssignmentResponse> PhanCongHienTai);

public sealed class CreateTeachingAssignmentRequest
{
    [Required, MaxLength(30)] public string MaLopHocPhan { get; init; } = string.Empty;
    [Required, MaxLength(30)] public string MaMonHoc { get; init; } = string.Empty;
    [Required, MaxLength(30)] public string MaHocKy { get; init; } = string.Empty;
    [Required, MaxLength(30)] public string MaPhong { get; init; } = string.Empty;
    [Range(1, 500)] public int SiSoToiDa { get; init; } = 70;
    [Range(2, 8)] public int ThuTrongTuan { get; init; }
    [Range(1, 20)] public int TietBatDau { get; init; }
    [Range(1, 20)] public int TietKetThuc { get; init; }
}

public sealed record AttendanceSessionResponse(
    string MaBuoiHoc,
    DateTime NgayHocCuThe,
    string TrangThaiBuoiHoc,
    bool DaChot);

/// <summary>Thống kê điểm danh của một buổi học, dùng cho trang lịch sử.</summary>
public sealed record AttendanceSessionStatisticsResponse(
    string MaBuoiHoc,
    DateTime NgayHocCuThe,
    string TrangThaiBuoiHoc,
    bool DaChot,
    bool LaHomNay,
    int SiSoLop,
    int CoMat,
    int DiTre,
    int CoPhep,
    int Vang,
    int ChuaDiemDanh);

public sealed record ClassAttendanceHistoryResponse(
    TeachingAssignmentResponse LopHocPhan,
    string KhungGioTiet,
    IReadOnlyCollection<AttendanceSessionStatisticsResponse> BuoiHoc);

public sealed record StudentAttendanceResponse(
    string MaSinhVien,
    string HoTen,
    string Email,
    string? SoDienThoai,
    string TrangThaiBuoiHoc,
    DateTime? ThoiGianCheckIn,
    int CoMat,
    int DiTre,
    int CoPhep,
    int Vang,
    int TongSoBuoi,
    string? TrangThaiCanhBao);

public sealed record LecturerClassWorkspaceResponse(
    TeachingAssignmentResponse LopHocPhan,
    IReadOnlyCollection<AttendanceSessionResponse> BuoiHoc,
    string? MaBuoiHocDangChon,
    IReadOnlyCollection<StudentAttendanceResponse> SinhVien,
    // Chỉ buổi diễn ra hôm nay và đang trong giờ lên lớp mới được sửa điểm danh.
    bool CoTheDiemDanh,
    string? LyDoKhongDiemDanh,
    string KhungGioTiet,
    DateTime? MoDiemDanhLuc,
    DateTime? DongDiemDanhLuc);

public sealed class UpdateAttendanceRequest
{
    [Required] public string TrangThai { get; init; } = string.Empty;
}
