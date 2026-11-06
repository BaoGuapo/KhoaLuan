namespace QuanLySinhVien.Api.Contracts.Academic;

/// <summary>Số buổi điểm danh của sinh viên trong lớp học phần, kèm mức cảnh báo chuyên cần.</summary>
public sealed record AttendanceTallyResponse(
    int CoMat,
    int DiTre,
    int CoPhep,
    int Vang,
    int TongSoBuoi,
    string? MucCanhBao);

public sealed record CourseResultResponse(
    string MaMonHoc,
    string TenMonHoc,
    string? MaLopHocPhan,
    int SoTinChi,
    // Chỉ có khi điểm đã được công bố. Điểm tổng kết null với kết quả học vụ nhập thẳng điểm chữ.
    decimal? DiemTongKet,
    string? DiemChu,
    decimal? Diem4,
    // Dat, KhongDat hoặc ChuaCoDiem.
    string TrangThai,
    AttendanceTallyResponse? ChuyenCan);

public sealed record SemesterResultResponse(
    string MaHocKy,
    string TenHocKy,
    DateTime? NgayBatDau,
    // Null khi học kỳ chưa có môn nào được công bố điểm.
    decimal? GpaHocKy,
    int TinChiDat,
    decimal? GpaTichLuy,
    int TinChiTichLuy,
    IReadOnlyCollection<CourseResultResponse> MonHoc);

public sealed record AcademicResultResponse(
    decimal? GpaTichLuy,
    int TinChiTichLuy,
    int TinChiChuaCoDiem,
    // Học kỳ mới nhất đứng đầu.
    IReadOnlyCollection<SemesterResultResponse> HocKy);

public sealed record ClassGradeRowResponse(
    string MaSinhVien,
    string HoTen,
    decimal? DiemTongKet,
    string? DiemChu,
    // Sinh viên bị cấm thi vì chuyên cần thì nhận F, không cần nhập điểm.
    bool CamThi,
    int SoBuoiVang);

public sealed record ClassGradeSheetResponse(
    string MaLopHocPhan,
    string MaMonHoc,
    string TenMonHoc,
    string MaHocKy,
    string TenHocKy,
    string HoTenGiangVien,
    bool DaCongBo,
    DateTime? NhapLucUtc,
    IReadOnlyCollection<ClassGradeRowResponse> SinhVien);

public sealed class SaveClassGradesRequest
{
    public List<GradeEntry> Diem { get; init; } = [];

    public sealed class GradeEntry
    {
        public string MaSinhVien { get; init; } = "";
        public decimal? DiemTongKet { get; init; }
    }
}
