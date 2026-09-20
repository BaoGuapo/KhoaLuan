using QuanLySinhVien.Api.Contracts.Auth;

namespace QuanLySinhVien.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(Guid userId, string refreshToken, CancellationToken cancellationToken);
}
