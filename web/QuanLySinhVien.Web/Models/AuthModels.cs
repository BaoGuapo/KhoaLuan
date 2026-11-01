using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã tài khoản.")]
    [Display(Name = "Mã tài khoản")]
    public string MaDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }
}

<<<<<<< HEAD
=======
public sealed class FaceVerificationViewModel
{
    public string MaDangNhap { get; set; } = string.Empty;
    public string DescriptorsJson { get; set; } = string.Empty;
}

>>>>>>> origin/main
public sealed class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password), Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

<<<<<<< HEAD
public sealed record AuthApiResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, string UserName, IReadOnlyCollection<string> Roles);
=======
public sealed record AuthApiResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    string UserName,
    string FullName,
    IReadOnlyCollection<string> Roles,
    bool MustChangePassword);
>>>>>>> origin/main
