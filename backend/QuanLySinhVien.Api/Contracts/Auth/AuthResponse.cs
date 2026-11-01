namespace QuanLySinhVien.Api.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    string UserName,
    string FullName,
    IReadOnlyCollection<string> Roles,
    bool MustChangePassword);
