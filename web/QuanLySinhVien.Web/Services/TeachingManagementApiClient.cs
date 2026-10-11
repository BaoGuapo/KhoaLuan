using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Services;

public interface ITeachingManagementApiClient
{
    Task<ApiResult<LecturerAssignmentWorkspaceModel>> GetAssignmentWorkspaceAsync(string token, string lecturerCode, CancellationToken ct);
    Task<ApiResult> CreateAssignmentAsync(string token, CreateTeachingAssignmentViewModel model, CancellationToken ct);
    Task<ApiResult<IReadOnlyCollection<TeachingAssignmentModel>>> GetMyAssignmentsAsync(string token, CancellationToken ct);
    Task<ApiResult<LecturerClassWorkspaceModel>> GetClassWorkspaceAsync(string token, string classCode, string? sessionCode, CancellationToken ct);
    Task<ApiResult> UpdateAttendanceAsync(string token, string sessionCode, string studentCode, string status, CancellationToken ct);
    Task<ApiResult> FinalizeAttendanceAsync(string token, string sessionCode, CancellationToken ct);
}

public sealed class TeachingManagementApiClient(HttpClient httpClient) : ITeachingManagementApiClient
{
    public Task<ApiResult<LecturerAssignmentWorkspaceModel>> GetAssignmentWorkspaceAsync(
        string token,
        string lecturerCode,
        CancellationToken ct) =>
        GetAsync<LecturerAssignmentWorkspaceModel>(token,
            $"api/v1/giang-day/giang-vien/{Uri.EscapeDataString(lecturerCode)}/phan-cong", ct);

    public Task<ApiResult> CreateAssignmentAsync(
        string token,
        CreateTeachingAssignmentViewModel model,
        CancellationToken ct) =>
        SendAsync(token, HttpMethod.Post,
            $"api/v1/giang-day/giang-vien/{Uri.EscapeDataString(model.MaGiangVien)}/phan-cong", model, ct);

    public async Task<ApiResult<IReadOnlyCollection<TeachingAssignmentModel>>> GetMyAssignmentsAsync(
        string token,
        CancellationToken ct)
    {
        var result = await GetAsync<List<TeachingAssignmentModel>>(token, "api/v1/giang-day/cua-toi", ct);
        return new ApiResult<IReadOnlyCollection<TeachingAssignmentModel>>(result.Succeeded, result.Value, result.Error);
    }

    public Task<ApiResult<LecturerClassWorkspaceModel>> GetClassWorkspaceAsync(
        string token,
        string classCode,
        string? sessionCode,
        CancellationToken ct)
    {
        var path = $"api/v1/giang-day/lop/{Uri.EscapeDataString(classCode)}";
        if (!string.IsNullOrWhiteSpace(sessionCode))
            path += $"?maBuoiHoc={Uri.EscapeDataString(sessionCode)}";
        return GetAsync<LecturerClassWorkspaceModel>(token, path, ct);
    }

    public Task<ApiResult> UpdateAttendanceAsync(
        string token,
        string sessionCode,
        string studentCode,
        string status,
        CancellationToken ct) =>
        SendAsync(token, HttpMethod.Put,
            $"api/v1/giang-day/buoi-hoc/{Uri.EscapeDataString(sessionCode)}/sinh-vien/{Uri.EscapeDataString(studentCode)}/diem-danh",
            new { trangThai = status }, ct);

    public Task<ApiResult> FinalizeAttendanceAsync(string token, string sessionCode, CancellationToken ct) =>
        SendAsync(token, HttpMethod.Post,
            $"api/v1/giang-day/buoi-hoc/{Uri.EscapeDataString(sessionCode)}/chot-diem-danh", new { }, ct);

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

    private async Task<ApiResult> SendAsync(
        string token,
        HttpMethod method,
        string path,
        object body,
        CancellationToken ct)
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
            if (document.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? "Yêu cầu thất bại.";
            if (document.RootElement.TryGetProperty("title", out var title))
                return title.GetString() ?? "Yêu cầu thất bại.";
        }
        catch (JsonException) { }
        return $"Yêu cầu thất bại ({(int)response.StatusCode}).";
    }
}
