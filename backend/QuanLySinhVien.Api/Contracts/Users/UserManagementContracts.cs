using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Users;

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record UserSummaryResponse(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    bool MustChangePassword,
    DateTime CreatedAtUtc,
    string? ProfileType,
    string? ProfileCode);

public sealed record UserDetailResponse(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    bool MustChangePassword,
    DateTime CreatedAtUtc,
    string? ProfileType,
    string? ProfileCode,
    string? PhoneNumber,
    AcademicProfileResponse? AcademicProfile);

public sealed record AcademicProfileResponse(
    string? AnhDaiDien,
    string? GioiTinh,
    DateTime? NgaySinh,
    string? NoiSinh,
    string? DanToc,
    string? TonGiao,
    string? QuocTich,
    string? SoCCCD,
    DateTime? NgayCapCCCD,
    string? NoiCapCCCD,
    string? DiaChiLienHe,
    string? HoKhauThuongTru,
    string? CoSo,
    string? Khoa,
    string? ChuyenNganh,
    string? Nganh,
    string? LopHoc,
    string? KhoaHoc,
    string? BacDaoTao,
    string? LoaiHinhDaoTao,
    DateTime? NgayVaoTruong,
    DateTime? NgayBatDauCongTac,
    string? TrinhDoHocVan,
    string? ChucDanh);

public sealed record MyProfileResponse(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    IReadOnlyCollection<string> Roles,
    string? ProfileType,
    string? ProfileCode,
    string? PhoneNumber);

public sealed class CreateStaffAccountRequest
{
    [Required, MaxLength(100)] public string UserName { get; init; } = string.Empty;
    [Required, MaxLength(200)] public string FullName { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(256)] public string Email { get; init; } = string.Empty;
    [Required, MinLength(8), MaxLength(128)] public string InitialPassword { get; init; } = string.Empty;
    [Required] public string Role { get; init; } = string.Empty;
}

public sealed class UpdateAccountStatusRequest
{
    public bool IsActive { get; init; }
}

public sealed class ResetPasswordRequest
{
    [Required, MinLength(8), MaxLength(128)] public string TemporaryPassword { get; init; } = string.Empty;
}

public sealed class UpdateUserRoleRequest
{
    [Required] public string Role { get; init; } = string.Empty;
}

public sealed class UpdateMyProfileRequest
{
    [Required, MaxLength(200)] public string FullName { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(256)] public string Email { get; init; } = string.Empty;
    [MaxLength(30)] public string? PhoneNumber { get; init; }
    [MaxLength(500)] public string? AnhDaiDien { get; init; }
    [MaxLength(20)] public string? GioiTinh { get; init; }
    public DateTime? NgaySinh { get; init; }
    [MaxLength(200)] public string? NoiSinh { get; init; }
    [MaxLength(100)] public string? DanToc { get; init; }
    [MaxLength(100)] public string? TonGiao { get; init; }
    [MaxLength(100)] public string? QuocTich { get; init; }
    [MaxLength(500)] public string? DiaChiLienHe { get; init; }
    [MaxLength(500)] public string? HoKhauThuongTru { get; init; }
}

public class PersonalAcademicProfileRequest
{
    [MaxLength(500)] public string? AnhDaiDien { get; init; }
    [MaxLength(20)] public string? GioiTinh { get; init; }
    public DateTime? NgaySinh { get; init; }
    [MaxLength(200)] public string? NoiSinh { get; init; }
    [MaxLength(100)] public string? DanToc { get; init; }
    [MaxLength(100)] public string? TonGiao { get; init; }
    [MaxLength(100)] public string? QuocTich { get; init; }
    [MaxLength(30)] public string? SoCCCD { get; init; }
    public DateTime? NgayCapCCCD { get; init; }
    [MaxLength(200)] public string? NoiCapCCCD { get; init; }
    [MaxLength(500)] public string? DiaChiLienHe { get; init; }
    [MaxLength(500)] public string? HoKhauThuongTru { get; init; }
}

public sealed class UpdateSinhVienRequest : PersonalAcademicProfileRequest
{
    [Required, MaxLength(200)] public string HoTen { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(256)] public string Email { get; init; } = string.Empty;
    [MaxLength(30)] public string? SoDienThoai { get; init; }
    [MaxLength(100)] public string? KhuVuc { get; init; }
    [MaxLength(50)] public string? MaHoSo { get; init; }
    public DateTime? NgayVaoTruong { get; init; }
    [MaxLength(100)] public string? LopHoc { get; init; }
    [MaxLength(200)] public string? CoSo { get; init; }
    [MaxLength(100)] public string? BacDaoTao { get; init; }
    [MaxLength(100)] public string? LoaiHinhDaoTao { get; init; }
    [MaxLength(200)] public string? Khoa { get; init; }
    [MaxLength(200)] public string? Nganh { get; init; }
    [MaxLength(200)] public string? ChuyenNganh { get; init; }
    [MaxLength(50)] public string? KhoaHoc { get; init; }
    [MaxLength(100)] public string? DoiTuong { get; init; }
    [MaxLength(100)] public string? DienChinhSach { get; init; }
    public DateTime? NgayVaoDoan { get; init; }
    public DateTime? NgayVaoDang { get; init; }
}

public sealed class UpdateGiangVienRequest : PersonalAcademicProfileRequest
{
    [Required, MaxLength(200)] public string HoTen { get; init; } = string.Empty;
    [EmailAddress, MaxLength(256)] public string? Email { get; init; }
    [MaxLength(20)] public string? SoDienThoai { get; init; }
    public DateTime? NgayBatDauCongTac { get; init; }
    [MaxLength(200)] public string? CoSo { get; init; }
    [MaxLength(200)] public string? Khoa { get; init; }
    [MaxLength(200)] public string? ChuyenNganh { get; init; }
    [MaxLength(100)] public string? TrinhDoHocVan { get; init; }
    [MaxLength(100)] public string? ChucDanh { get; init; }
    public DateTime? NgayVaoDang { get; init; }
}
