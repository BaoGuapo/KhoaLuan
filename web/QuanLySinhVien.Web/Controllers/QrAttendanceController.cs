using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Controllers;

[Authorize, Route("diem-danh")]
public sealed class QrAttendanceController(IHttpClientFactory clients) : Controller
{
    private const string Managers = RoleNames.GiangVien + "," + RoleNames.Admin + "," + RoleNames.CanBoQuanLy;
    private bool CanApproveFaces => User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.CanBoQuanLy);
    [HttpGet(""), Authorize(Roles = Managers + "," + RoleNames.SinhVien)]
    public IActionResult Index(string? maBuoiHoc) { ViewData["Lesson"] = maBuoiHoc; return View(); }
    [HttpGet("ho-so"), Authorize(Roles = RoleNames.SinhVien)]
    public IActionResult Profile() { ViewData["Enrollment"] = true; return View("Index"); }

    [HttpGet("api/{operation}")]
    public Task<IActionResult> Read(string operation, Guid? id, CancellationToken ct) => Forward(operation switch
    {
        "lessons" => "buoi-hoc", "qr" when id.HasValue => $"phien/{id}/qr",
        "results" when id.HasValue => $"phien/{id}/ket-qua", "profiles" => "ho-so/cho-duyet", "profile" => "ho-so", "my-results" => "ket-qua-cua-toi",
        _ => null
    }, HttpMethod.Get, null, ct);

    [HttpPost("api/{operation}"), ValidateAntiForgeryToken, RequestSizeLimit(4_500_000)]
    public Task<IActionResult> Write(string operation, [FromBody] JsonElement body, string? code, Guid? id, CancellationToken ct)
    {
        var safeCode = Uri.EscapeDataString(code ?? "");
        return Forward(operation switch
        {
            "open" when !string.IsNullOrWhiteSpace(code) => $"buoi-hoc/{safeCode}/mo",
            "close" when id.HasValue => $"phien/{id}/dong", "scan" => "quet", "confirm" => "xac-nhan",
            "enrollment-challenge" => "ho-so/thu-thach", "enroll" => "ho-so",
            "guidance" => "huong-dan",
            "approval-challenge" when !string.IsNullOrWhiteSpace(code) => $"ho-so/{safeCode}/thu-thach-duyet",
            "approve" when !string.IsNullOrWhiteSpace(code) => $"ho-so/{safeCode}/duyet",
            "revoke" when !string.IsNullOrWhiteSpace(code) => $"ho-so/{safeCode}/huy", _ => null
        }, HttpMethod.Post, body.GetRawText(), ct);
    }

    private async Task<IActionResult> Forward(string? path, HttpMethod method, string? body, CancellationToken ct)
    {
        if (path is null) return BadRequest();
        if ((path == "ho-so/cho-duyet" || path.EndsWith("/duyet") || path.EndsWith("/thu-thach-duyet") || path.EndsWith("/huy")) && !CanApproveFaces) return Forbid();
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token)) return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn." });
        using var request = new HttpRequestMessage(method, "api/v1/qr-diem-danh/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        try
        {
            using var response = await clients.CreateClient("Attendance").SendAsync(request, ct);
            Response.Headers.CacheControl = "no-store";
            return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = "application/json; charset=utf-8",
                Content = await response.Content.ReadAsStringAsync(ct) };
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        { return StatusCode(503, new { message = "Không thể kết nối dịch vụ điểm danh. Vui lòng thử lại." }); }
    }
}
