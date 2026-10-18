using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Services;

public sealed class StudentAcademicApiClient(HttpClient client)
{
    public async Task<StudentAcademicWorkspace> LoadAsync(string token, CancellationToken ct, bool includeOffers = true)
    {
        try
        {
            var offers = includeOffers ? Get<Offers>(token, "api/v1/dang-ky-hoc-phan/danh-sach", ct) : Task.FromResult(new Offers());
            var enrolled = Get<List<StudentCourseClass>>(token, "api/v1/dang-ky-hoc-phan/da-dang-ky", ct);
            var semesters = Get<List<StudentSemester>>(token, "api/v1/hoc-ky", ct);
            await Task.WhenAll(offers, enrolled, semesters);
            return new() { Available = (await offers).Data, Registered = await enrolled, Semesters = await semesters };
        }
        catch (HttpRequestException ex)
        { return new() { Error = ex.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." : "Chưa tải được dữ liệu học phần. Vui lòng thử lại sau." }; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { return new() { Error = "Kết nối đến hệ thống đang chậm. Vui lòng thử lại." }; }
        catch (JsonException) { return new() { Error = "Dữ liệu học phần chưa hợp lệ. Vui lòng liên hệ phòng đào tạo." }; }
    }
    public async Task<ApiResult> RegisterAsync(string token, string code, CancellationToken ct)
    {
        try
        {
            using var request = Request(token, HttpMethod.Post, "api/v1/dang-ky-hoc-phan/sinh-vien-tu-dang-ky/" + Uri.EscapeDataString(code));
            using var response = await client.SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return new(true);
            try
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                if (json.RootElement.TryGetProperty("message", out var message)) return new(false, message.GetString());
            }
            catch (JsonException) { }
            return new(false, "Không đăng ký được lớp. Vui lòng tải lại danh sách và thử lại.");
        }
        catch (HttpRequestException) { return new(false, "Không thể kết nối backend. Chưa xác nhận đăng ký; hãy tải lại kết quả trước khi thử tiếp."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { return new(false, "Chưa nhận được kết quả đăng ký. Hãy tải lại danh sách đã đăng ký trước khi thử tiếp."); }
    }
    private async Task<T> Get<T>(string token, string url, CancellationToken ct)
    {
        using var request = Request(token, HttpMethod.Get, url); using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct) ?? throw new JsonException();
    }
    private static HttpRequestMessage Request(string token, HttpMethod method, string url)
    { var request = new HttpRequestMessage(method, url); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); return request; }
    private sealed class Offers { public List<StudentCourseClass> Data { get; set; } = []; }
}
