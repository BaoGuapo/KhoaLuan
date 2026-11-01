namespace QuanLySinhVien.Api.Contracts.Auth;

<<<<<<< HEAD
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, string UserName, IReadOnlyCollection<string> Roles);
=======
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    string UserName,
    string FullName,
    IReadOnlyCollection<string> Roles,
    bool MustChangePassword);
>>>>>>> origin/main
