using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Training;

public sealed record CreateMajorRequest(
    [Required, StringLength(50), RegularExpression(@"^[A-Za-z0-9_-]+$")] string MaNganh,
    [Required, StringLength(100)] string TenNganh);
public sealed record CreateIntakeRequest(
    [Required, StringLength(50), RegularExpression(@"^[A-Za-z0-9_-]+$")] string MaKhoa,
    [Range(2000, 2100)] int NamBatDau,
    [Range(2000, 2110)] int NamKetThuc);
public sealed record CreateSemesterRequest(
    [Required, StringLength(30), RegularExpression(@"^[A-Za-z0-9_-]+$")] string MaHocKy,
    [Required, StringLength(100)] string TenHocKy,
    DateTime NgayBatDau, DateTime NgayKetThuc);
public sealed record CurriculumCourseRequest(
    [Required, StringLength(50)] string MaMonHoc,
    [Range(1, 12)] int ThuTuHocKy,
    bool BatBuoc);
public sealed record CreateCurriculumRequest(
    [Required, StringLength(50)] string MaNganh,
    [Required, StringLength(50)] string MaKhoa,
    [Required, StringLength(200)] string TenKhoaDaoTao,
    [Required, StringLength(200)] string TenChuongTrinh,
    [MinLength(1), MaxLength(200)] List<CurriculumCourseRequest> MonHocs);
public sealed record ApplySemesterRequest(
    Guid KeHoachDaoTaoId,
    [Required, StringLength(50)] string MaHocKy,
    [Range(1, 12)] int ThuTuHocKy);
public sealed record OpenClassesRequest(
    [Required, StringLength(50)] string MaMonHoc,
    [Required, StringLength(24), RegularExpression(@"^[A-Za-z0-9_-]+$")] string TienToMaLop,
    [Range(1, 50)] int SoLop,
    [Range(1, 500)] int SiSoToiDa);
public sealed record AssignPlannedClassRequest(
    [Required, StringLength(30)] string MaGiangVien,
    [Required, StringLength(30)] string MaPhong,
    [Range(2, 8)] int ThuTrongTuan,
    [Range(1, 15)] int TietBatDau,
    [Range(1, 15)] int TietKetThuc);
public sealed class TrainingOption
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
