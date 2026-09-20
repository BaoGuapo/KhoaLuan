namespace QuanLySinhVien.Api.Contracts.Auth;

public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, string UserName, IReadOnlyCollection<string> Roles);
