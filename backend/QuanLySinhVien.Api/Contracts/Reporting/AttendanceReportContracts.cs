namespace QuanLySinhVien.Api.Contracts.Reporting;

/// <summary>Số liệu tổng hợp của một học kỳ, hiển thị ở các ô tóm tắt đầu trang.</summary>
public sealed record AttendanceOverviewResponse(
    int SoSinhVien,
    int SoLopHocPhan,
    int SoMonHoc,
    int SoBuoiDaDienRa,
    int TongSoBuoi,
    int CoMat,
    int DiTre,
    int CoPhep,
    int Vang,
    int ChuaDiemDanh,
    double TyLeChuyenCan);

public sealed record SubjectAttendanceRow(
    string MaMonHoc,
    string TenMonHoc,
    int SoLop,
    int SoSinhVien,
    int CoMat,
    int DiTre,
    int CoPhep,
    int Vang,
    int ChuaDiemDanh,
    // Không có lượt dự kiến thì tỷ lệ chuyên cần không có nghĩa, phải hiển thị là chưa có số liệu.
    int SoLuotDuKien,
    double TyLeChuyenCan);

public sealed record ClassAttendanceRow(
    string MaLopHocPhan,
    string MaMonHoc,
    string TenMonHoc,
    string HoTenGiangVien,
    int SiSo,
    int SoBuoiDaDienRa,
    int CoMat,
    int DiTre,
    int CoPhep,
    int Vang,
    int ChuaDiemDanh,
    int SoLuotDuKien,
    double TyLeChuyenCan,
    int SoSinhVienCanhBao);

/// <summary>Tỷ lệ chuyên cần theo từng ngày học, dùng vẽ đường xu hướng.</summary>
public sealed record DailyAttendanceRow(
    DateTime Ngay,
    int SoBuoi,
    int SoLuotDuKien,
    int CoMatVaDiTre,
    double TyLeChuyenCan);

public sealed record AttendanceReportResponse(
    string MaHocKy,
    string TenHocKy,
    DateTime? NgayBatDau,
    DateTime? NgayKetThuc,
    AttendanceOverviewResponse TongQuan,
    IReadOnlyCollection<SubjectAttendanceRow> TheoMonHoc,
    IReadOnlyCollection<ClassAttendanceRow> TheoLopHocPhan,
    IReadOnlyCollection<DailyAttendanceRow> TheoNgay);
