using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Services;

public sealed class AttendanceReportApiClient(HttpClient client)
{
    public Task<ApiResult<List<SemesterOptionModel>>> SemestersAsync(string token, CancellationToken ct) =>
        GetAsync<List<SemesterOptionModel>>(token, "api/v1/hoc-ky", ct);

    public Task<ApiResult<AttendanceReportModel>> ReportAsync(string token, string maHocKy, CancellationToken ct) =>
        GetAsync<AttendanceReportModel>(token,
            $"api/v1/bao-cao/thong-ke-diem-danh?maHocKy={Uri.EscapeDataString(maHocKy)}", ct);

    public async Task<ApiResult<byte[]>> ExcelAsync(string token, string maHocKy, CancellationToken ct)
    {
        using var request = Request(token,
            $"api/v1/bao-cao/thong-ke-diem-danh/xuat-excel?maHocKy={Uri.EscapeDataString(maHocKy)}");
        try
        {
            using var response = await client.SendAsync(request, ct);
            return response.IsSuccessStatusCode
                ? new(true, await response.Content.ReadAsByteArrayAsync(ct))
                : new(false, default, await Error(response, ct));
        }
        catch (HttpRequestException) { return new(false, default, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new(false, default, "Backend API phản hồi quá thời gian."); }
    }

    private async Task<ApiResult<T>> GetAsync<T>(string token, string endpoint, CancellationToken ct)
    {
        using var request = Request(token, endpoint);
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return new(false, default, await Error(response, ct));
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return new(value is not null, value, value is null ? "Không có dữ liệu." : null);
        }
        catch (HttpRequestException) { return new(false, default, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new(false, default, "Backend API phản hồi quá thời gian."); }
    }

    private static HttpRequestMessage Request(string token, string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<string> Error(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (json.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? "Yêu cầu thất bại.";
        }
        catch (JsonException) { }
        return $"Yêu cầu thất bại ({(int)response.StatusCode}).";
    }
}
