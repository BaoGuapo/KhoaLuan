using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Services;

public interface IFaceRecognitionApiClient
{
    Task<ApiResult<FaceProfileMetadataModel>> GetMyProfileAsync(string token, CancellationToken ct);
    Task<ApiResult> SaveMyProfileAsync(string token, IReadOnlyCollection<double[]> descriptors, bool replace, CancellationToken ct);
    Task<ApiResult<FaceAttendanceResultModel>> RecognizeAttendanceAsync(
        string token,
        string sessionCode,
        IReadOnlyCollection<double[]> descriptors,
        CancellationToken ct);
    Task<ApiResult> VerifyLoginFaceAsync(string token, IReadOnlyCollection<double[]> descriptors, CancellationToken ct);
}

public sealed class FaceRecognitionApiClient(HttpClient httpClient) : IFaceRecognitionApiClient
{
    public async Task<ApiResult> VerifyLoginFaceAsync(
        string token,
        IReadOnlyCollection<double[]> descriptors,
        CancellationToken ct)
    {
        using var request = CreateRequest(token, HttpMethod.Post, "api/auth/verify-face");
        request.Content = JsonContent.Create(new { descriptors });
        using var response = await httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode
            ? new ApiResult(true)
            : new ApiResult(false, await ReadErrorAsync(response, ct));
    }

    public Task<ApiResult<FaceProfileMetadataModel>> GetMyProfileAsync(string token, CancellationToken ct) =>
        GetAsync<FaceProfileMetadataModel>(token, "api/v1/me/ho-so-khuon-mat", ct);

    public async Task<ApiResult> SaveMyProfileAsync(
        string token,
        IReadOnlyCollection<double[]> descriptors,
        bool replace,
        CancellationToken ct)
    {
        using var request = CreateRequest(
            token,
            replace ? HttpMethod.Put : HttpMethod.Post,
            replace ? "api/v1/me/ho-so-khuon-mat" : "api/v1/me/ho-so-khuon-mat/dang-ky");
        request.Content = JsonContent.Create(new { descriptors });
        using var response = await httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode
            ? new ApiResult(true)
            : new ApiResult(false, await ReadErrorAsync(response, ct));
    }

    public async Task<ApiResult<FaceAttendanceResultModel>> RecognizeAttendanceAsync(
        string token,
        string sessionCode,
        IReadOnlyCollection<double[]> descriptors,
        CancellationToken ct)
    {
        using var request = CreateRequest(
            token,
            HttpMethod.Post,
            $"api/v1/giang-day/buoi-hoc/{Uri.EscapeDataString(sessionCode)}/nhan-dien-khuon-mat");
        request.Content = JsonContent.Create(new { descriptors });
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return new ApiResult<FaceAttendanceResultModel>(false, null, await ReadErrorAsync(response, ct));
        var value = await response.Content.ReadFromJsonAsync<FaceAttendanceResultModel>(cancellationToken: ct);
        return value is null
            ? new ApiResult<FaceAttendanceResultModel>(false, null, "Backend trả về dữ liệu rỗng.")
            : new ApiResult<FaceAttendanceResultModel>(true, value);
    }

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
