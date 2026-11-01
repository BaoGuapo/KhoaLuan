using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Services;

public interface IAuthApiClient
{
    Task<AuthApiResponse?> LoginAsync(LoginViewModel model, CancellationToken cancellationToken);
    Task<bool> ChangePasswordAsync(string accessToken, ChangePasswordViewModel model, CancellationToken cancellationToken);
    Task LogoutAsync(string accessToken, string refreshToken, CancellationToken cancellationToken);
}

public sealed class AuthApiClient(HttpClient httpClient) : IAuthApiClient
{
    public async Task<AuthApiResponse?> LoginAsync(LoginViewModel model, CancellationToken cancellationToken)
    {
        // Backend receives UserNameOrEmail for compatibility; its value is the
        // MaSinhVien or MaGiangVien entered on the web form.
        var response = await httpClient.PostAsJsonAsync("api/auth/login", new
        {
            UserNameOrEmail = model.MaDangNhap.Trim(),
            model.Password
        }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthApiResponse>(cancellationToken: cancellationToken);
    }

    public async Task<bool> ChangePasswordAsync(string accessToken, ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password")
        {
            Content = JsonContent.Create(new { model.CurrentPassword, model.NewPassword })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task LogoutAsync(string accessToken, string refreshToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout")
        {
            Content = JsonContent.Create(new { refreshToken })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await httpClient.SendAsync(request, cancellationToken);
    }
}
