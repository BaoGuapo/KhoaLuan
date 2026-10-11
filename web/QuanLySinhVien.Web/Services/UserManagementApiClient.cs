using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Services;

public sealed record ApiResult(bool Succeeded, string? Error = null);
public sealed record ApiResult<T>(bool Succeeded, T? Value = default, string? Error = null);

public interface IUserManagementApiClient
{
    Task<ApiResult<PagedApiResponse<UserSummaryModel>>> GetUsersAsync(string token, string? search, string? role, bool? isActive, int page, CancellationToken ct);
    Task<ApiResult<UserDetailModel>> GetUserAsync(string token, Guid id, CancellationToken ct);
    Task<ApiResult<UserDetailModel>> GetMeAsync(string token, CancellationToken ct);
    Task<ApiResult> UpdateMeAsync(string token, PersonalProfileEditViewModel model, CancellationToken ct);
    Task<ApiResult> CreateStaffAsync(string token, CreateStaffAccountViewModel model, CancellationToken ct);
    Task<ApiResult> SetStatusAsync(string token, Guid id, bool isActive, CancellationToken ct);
    Task<ApiResult> ResetPasswordAsync(string token, Guid id, string password, CancellationToken ct);
    Task<ApiResult> ChangeRoleAsync(string token, Guid id, string role, CancellationToken ct);
    Task<ApiResult<IReadOnlyCollection<SinhVienModel>>> GetStudentsAsync(string token, string? search, CancellationToken ct);
    Task<ApiResult<SinhVienModel>> GetStudentAsync(string token, string code, CancellationToken ct);
    Task<ApiResult> CreateStudentAsync(string token, SinhVienFormViewModel model, CancellationToken ct);
    Task<ApiResult> UpdateStudentAsync(string token, string code, SinhVienFormViewModel model, CancellationToken ct);
    Task<ApiResult> ProvisionStudentAsync(string token, string code, string password, CancellationToken ct);
    Task<ApiResult<IReadOnlyCollection<GiangVienModel>>> GetLecturersAsync(string token, string? search, string? khoa, string? chuyenNganh, CancellationToken ct);
    Task<ApiResult<GiangVienModel>> GetLecturerAsync(string token, string code, CancellationToken ct);
    Task<ApiResult> CreateLecturerAsync(string token, GiangVienFormViewModel model, CancellationToken ct);
    Task<ApiResult> UpdateLecturerAsync(string token, string code, GiangVienFormViewModel model, CancellationToken ct);
    Task<ApiResult> ProvisionLecturerAsync(string token, string code, string email, string password, CancellationToken ct);
}

public sealed class UserManagementApiClient(HttpClient httpClient) : IUserManagementApiClient
{
    public Task<ApiResult<PagedApiResponse<UserSummaryModel>>> GetUsersAsync(
        string token, string? search, string? role, bool? isActive, int page, CancellationToken ct)
    {
        var values = new List<string> { $"page={Math.Max(1, page)}", "pageSize=20" };
        if (!string.IsNullOrWhiteSpace(search)) values.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(role)) values.Add($"role={Uri.EscapeDataString(role)}");
        if (isActive.HasValue) values.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        return GetAsync<PagedApiResponse<UserSummaryModel>>(token, $"api/v1/nguoi-dung?{string.Join('&', values)}", ct);
    }

    public Task<ApiResult<UserDetailModel>> GetUserAsync(string token, Guid id, CancellationToken ct) =>
        GetAsync<UserDetailModel>(token, $"api/v1/nguoi-dung/{id}", ct);

    public Task<ApiResult<UserDetailModel>> GetMeAsync(string token, CancellationToken ct) =>
        GetAsync<UserDetailModel>(token, "api/v1/nguoi-dung/me", ct);

    public Task<ApiResult> UpdateMeAsync(string token, PersonalProfileEditViewModel model, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Put, "api/v1/nguoi-dung/me", new
        {
            model.FullName,
            model.Email,
            model.PhoneNumber,
            model.AnhDaiDien,
            model.GioiTinh,
            model.NgaySinh,
            model.NoiSinh,
            model.DanToc,
            model.TonGiao,
            model.QuocTich,
            model.DiaChiLienHe,
            model.HoKhauThuongTru
        }, ct);

    public Task<ApiResult> CreateStaffAsync(string token, CreateStaffAccountViewModel model, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Post, "api/v1/nguoi-dung", new
        {
            model.UserName,
            model.FullName,
            model.Email,
            model.InitialPassword,
            model.Role
        }, ct);

    public Task<ApiResult> SetStatusAsync(string token, Guid id, bool isActive, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Put, $"api/v1/nguoi-dung/{id}/trang-thai", new { isActive }, ct);

    public Task<ApiResult> ResetPasswordAsync(string token, Guid id, string password, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Post, $"api/v1/nguoi-dung/{id}/dat-lai-mat-khau", new { temporaryPassword = password }, ct);

    public Task<ApiResult> ChangeRoleAsync(string token, Guid id, string role, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Put, $"api/v1/nguoi-dung/{id}/vai-tro", new { role }, ct);

    public async Task<ApiResult<IReadOnlyCollection<SinhVienModel>>> GetStudentsAsync(string token, string? search, CancellationToken ct)
    {
        var path = "api/v1/sinh-vien" + (string.IsNullOrWhiteSpace(search) ? string.Empty : $"?search={Uri.EscapeDataString(search.Trim())}");
        var result = await GetAsync<List<SinhVienModel>>(token, path, ct);
        return new ApiResult<IReadOnlyCollection<SinhVienModel>>(result.Succeeded, result.Value, result.Error);
    }

    public Task<ApiResult<SinhVienModel>> GetStudentAsync(string token, string code, CancellationToken ct) =>
        GetAsync<SinhVienModel>(token, $"api/v1/sinh-vien/{Uri.EscapeDataString(code)}", ct);

    public Task<ApiResult> CreateStudentAsync(string token, SinhVienFormViewModel model, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Post, "api/v1/sinh-vien", model, ct);

    public Task<ApiResult> UpdateStudentAsync(string token, string code, SinhVienFormViewModel model, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Put, $"api/v1/sinh-vien/{Uri.EscapeDataString(code)}", model, ct);

    public Task<ApiResult> ProvisionStudentAsync(string token, string code, string password, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Post, $"api/v1/sinh-vien/{Uri.EscapeDataString(code)}/tai-khoan", new { initialPassword = password }, ct);

    public async Task<ApiResult<IReadOnlyCollection<GiangVienModel>>> GetLecturersAsync(
        string token,
        string? search,
        string? khoa,
        string? chuyenNganh,
        CancellationToken ct)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(khoa)) query.Add($"khoa={Uri.EscapeDataString(khoa.Trim())}");
        if (!string.IsNullOrWhiteSpace(chuyenNganh)) query.Add($"chuyenNganh={Uri.EscapeDataString(chuyenNganh.Trim())}");
        var path = "api/v1/giang-vien" + (query.Count == 0 ? string.Empty : $"?{string.Join('&', query)}");
        var result = await GetAsync<List<GiangVienModel>>(token, path, ct);
        return new ApiResult<IReadOnlyCollection<GiangVienModel>>(result.Succeeded, result.Value, result.Error);
    }

    public Task<ApiResult<GiangVienModel>> GetLecturerAsync(string token, string code, CancellationToken ct) =>
        GetAsync<GiangVienModel>(token, $"api/v1/giang-vien/{Uri.EscapeDataString(code)}", ct);

    public Task<ApiResult> CreateLecturerAsync(string token, GiangVienFormViewModel model, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Post, "api/v1/giang-vien", model, ct);

    public Task<ApiResult> UpdateLecturerAsync(string token, string code, GiangVienFormViewModel model, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Put, $"api/v1/giang-vien/{Uri.EscapeDataString(code)}", model, ct);

    public Task<ApiResult> ProvisionLecturerAsync(string token, string code, string email, string password, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Post, $"api/v1/giang-vien/{Uri.EscapeDataString(code)}/tai-khoan", new
        {
            email,
            initialPassword = password
        }, ct);

    private async Task<ApiResult<T>> GetAsync<T>(string token, string path, CancellationToken ct)
    {
        using var request = CreateRequest(token, HttpMethod.Get, path);
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return new ApiResult<T>(false, default, await ReadErrorAsync(response, ct));
        var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        return value is null
            ? new ApiResult<T>(false, default, "Backend trả về dữ liệu rỗng.")
            : new ApiResult<T>(true, value);
    }

    private async Task<ApiResult> SendAsync(string token, HttpMethod method, string path, object body, CancellationToken ct)
    {
        using var request = CreateRequest(token, method, path);
        request.Content = JsonContent.Create(body);
        using var response = await httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode
            ? new ApiResult(true)
            : new ApiResult(false, await ReadErrorAsync(response, ct));
    }

    private static HttpRequestMessage CreateRequest(string token, HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (document.RootElement.TryGetProperty("message", out var message)) return message.GetString() ?? "Yêu cầu thất bại.";
            if (document.RootElement.TryGetProperty("title", out var title)) return title.GetString() ?? "Yêu cầu thất bại.";
        }
        catch (JsonException) { }
        return $"Yêu cầu thất bại ({(int)response.StatusCode}).";
    }
}
