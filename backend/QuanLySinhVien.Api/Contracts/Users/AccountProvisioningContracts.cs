using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Users;

<<<<<<< HEAD
public sealed class CreateSinhVienRequest
=======
public sealed class CreateSinhVienRequest : PersonalAcademicProfileRequest
>>>>>>> origin/main
{
    [Required, MaxLength(30)] public string MaSinhVien { get; init; } = string.Empty;
    [Required, MaxLength(200)] public string HoTen { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(256)] public string Email { get; init; } = string.Empty;
    [MaxLength(30)] public string? SoDienThoai { get; init; }
<<<<<<< HEAD
}

public sealed class CreateGiangVienRequest
{
    [Required, MaxLength(30)] public string MaGiangVien { get; init; } = string.Empty;
    [Required, MaxLength(200)] public string HoTen { get; init; } = string.Empty;
=======
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

public sealed class CreateGiangVienRequest : PersonalAcademicProfileRequest
{
    [Required, MaxLength(30)] public string MaGiangVien { get; init; } = string.Empty;
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
>>>>>>> origin/main
}

public sealed class ProvisionSinhVienAccountRequest
{
    [Required, MinLength(8), MaxLength(128)] public string InitialPassword { get; init; } = string.Empty;
}

public sealed class ProvisionGiangVienAccountRequest
{
    [Required, EmailAddress, MaxLength(256)] public string Email { get; init; } = string.Empty;
    [Required, MinLength(8), MaxLength(128)] public string InitialPassword { get; init; } = string.Empty;
}

public sealed record ProvisionedAccountResponse(
    Guid UserId,
    string UserName,
    string FullName,
    string Email,
    string Role);
