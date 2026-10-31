using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Controllers;

[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
[Route("quan-ly/ho-so-khuon-mat")]
public sealed class FaceProfileManagementController(IHttpClientFactory clients) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("api/profiles")]
    public Task<IActionResult> Profiles(CancellationToken ct) => Forward("ho-so/cho-duyet", HttpMethod.Get, null, ct);

    [HttpGet("api/review")]
    public Task<IActionResult> Review(string code, CancellationToken ct) => Forward($"ho-so/{Uri.EscapeDataString(code)}/doi-chieu", HttpMethod.Get, null, ct);

    [HttpPost("api/{operation}"), ValidateAntiForgeryToken, RequestSizeLimit(4_500_000)]
    public Task<IActionResult> Write(string operation, [FromBody] JsonElement body, string? code, CancellationToken ct)
    {
        var safe = Uri.EscapeDataString(code ?? "");
        var path = operation switch
        {
            "approve" when !string.IsNullOrWhiteSpace(code) => $"ho-so/{safe}/duyet",
            "reject" when !string.IsNullOrWhiteSpace(code) => $"ho-so/{safe}/tu-choi",
            "identity-photo" when !string.IsNullOrWhiteSpace(code) => $"ho-so/{safe}/anh-the",
            "revoke" when !string.IsNullOrWhiteSpace(code) => $"ho-so/{safe}/huy",
            _ => null
        };
        return Forward(path, HttpMethod.Post, body.GetRawText(), ct);
    }

    private async Task<IActionResult> Forward(string? path, HttpMethod method, string? body, CancellationToken ct)
    {
        if (path is null) return BadRequest();
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token)) return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
        using var request = new HttpRequestMessage(method, "api/v1/qr-diem-danh/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        try
        {
            using var response = await clients.CreateClient("Attendance").SendAsync(request, ct);
            Response.Headers.CacheControl = "no-store";
            return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = "application/json; charset=utf-8", Content = await response.Content.ReadAsStringAsync(ct) };
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        { return StatusCode(503, new { message = "Không thể kết nối dịch vụ hồ sơ khuôn mặt. Vui lòng thử lại." }); }
    }
}
