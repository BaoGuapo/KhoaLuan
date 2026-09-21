using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Users;

public sealed class CreateSinhVienRequest
{
    [Required, MaxLength(30)] public string MaSinhVien { get; init; } = string.Empty;
    [Required, MaxLength(200)] public string HoTen { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(256)] public string Email { get; init; } = string.Empty;
    [MaxLength(30)] public string? SoDienThoai { get; init; }
}

public sealed class CreateGiangVienRequest
{
    [Required, MaxLength(30)] public string MaGiangVien { get; init; } = string.Empty;
    [Required, MaxLength(200)] public string HoTen { get; init; } = string.Empty;
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
