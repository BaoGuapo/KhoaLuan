using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Services;

public sealed class TrainingWorkflowApiClient(HttpClient client)
{
    public async Task<ApiResult<T>> GetAsync<T>(string token, string endpoint, CancellationToken ct)
    {
        using var request = Request(token, HttpMethod.Get, endpoint);
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return new(false, default, await Error(response, ct));
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return new(value is not null, value, value is null ? "Không có dữ liệu." : null);
        }
        catch (HttpRequestException) { return new(false, default, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new(false, default, "Backend phản hồi quá thời gian."); }
    }
    public async Task<ApiResult> PostAsync(string token, string endpoint, object body, CancellationToken ct)
    {
        using var request = Request(token, HttpMethod.Post, endpoint); request.Content = JsonContent.Create(body);
        try
        {
            using var response = await client.SendAsync(request, ct);
            return new(response.IsSuccessStatusCode, response.IsSuccessStatusCode ? null : await Error(response, ct));
        }
        catch (HttpRequestException) { return new(false, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new(false, "Backend phản hồi quá thời gian."); }
    }
    public async Task<ApiResult<CurriculumPreviewModel>> PreviewAsync(string token, IFormFile file, CancellationToken ct)
    {
        using var request = Request(token, HttpMethod.Post, "chuong-trinh/xem-truoc-excel");
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream(); content.Add(new StreamContent(stream), "file", Path.GetFileName(file.FileName)); request.Content = content;
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return new(false, default, await Error(response, ct));
            return new(true, await response.Content.ReadFromJsonAsync<CurriculumPreviewModel>(cancellationToken: ct));
        }
        catch (HttpRequestException) { return new(false, default, "Không thể kết nối backend."); }
        catch (TaskCanceledException) { return new(false, default, "Backend phản hồi quá thời gian."); }
    }
    public async Task<ApiResult<byte[]>> TemplateAsync(string token, CancellationToken ct)
    {
        using var request = Request(token, HttpMethod.Get, "chuong-trinh/mau-excel");
        try
        {
            using var response = await client.SendAsync(request, ct);
            return response.IsSuccessStatusCode ? new(true, await response.Content.ReadAsByteArrayAsync(ct)) : new(false, default, await Error(response, ct));
        }
        catch (HttpRequestException) { return new(false, default, "Không thể kết nối backend."); }
        catch (TaskCanceledException) { return new(false, default, "Backend phản hồi quá thời gian."); }
    }
    private static HttpRequestMessage Request(string token, HttpMethod method, string endpoint)
    { var request = new HttpRequestMessage(method, "api/v1/dao-tao/" + endpoint); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); return request; }
    private static async Task<string> Error(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (json.RootElement.TryGetProperty("message", out var message)) return message.GetString() ?? "Yêu cầu thất bại.";
            if (json.RootElement.TryGetProperty("errors", out var errors))
                return string.Join(" ", errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString()));
        }
        catch (JsonException) { }
        return response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại." : "Không thể xử lý yêu cầu. Kiểm tra kết nối database và migration backend.";
    }
}
