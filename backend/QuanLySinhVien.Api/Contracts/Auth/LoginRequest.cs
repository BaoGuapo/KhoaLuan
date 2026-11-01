using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Auth;

public sealed class LoginRequest
{
    [Required, MaxLength(256)] public string UserNameOrEmail { get; init; } = string.Empty;
    [Required, MinLength(8), MaxLength(128)] public string Password { get; init; } = string.Empty;
}
