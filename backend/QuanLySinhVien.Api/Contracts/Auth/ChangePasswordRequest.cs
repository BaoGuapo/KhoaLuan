using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Auth;

public sealed class ChangePasswordRequest
{
    [Required, MinLength(8), MaxLength(128)] public string CurrentPassword { get; init; } = string.Empty;
    [Required, MinLength(8), MaxLength(128)] public string NewPassword { get; init; } = string.Empty;
}
