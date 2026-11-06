using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Controllers;

/// <summary>
/// Chuyển tiếp các lời gọi từ trang quản lý đào tạo (môn học, ngành, khóa học, lớp học phần, giáo trình)
/// tới backend kèm access token trong phiên, để trình duyệt không cần giữ JWT.
/// </summary>
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy), Route("quan-ly-dao-tao/api")]
public sealed class TrainingApiProxyController(IHttpClientFactory clients) : Controller
{
    // Chỉ các API của tính năng quản lý đào tạo được đi qua proxy này.
    private static readonly HashSet<string> AllowedResources = new(StringComparer.OrdinalIgnoreCase)
    {
        "GiaoTrinh", "MonHoc", "LopHocPhan", "NganhDaoTao", "KhoaHoc", "GiangVien", "PhongHoc"
    };

    [AcceptVerbs("GET", "POST", "PUT", "DELETE"), Route("{resource}/{**rest}")]
    public async Task<IActionResult> Forward(string resource, string? rest, CancellationToken ct)
    {
        // Chặn "..": HttpClient sẽ chuẩn hóa đường dẫn và có thể thoát khỏi danh sách được phép.
        if (!AllowedResources.Contains(resource) || rest?.Split('/').Contains("..") == true) return NotFound();
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token)) return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn." });

        var path = string.IsNullOrEmpty(rest) ? $"api/{resource}" : $"api/{resource}/{rest}";
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), path + Request.QueryString);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (Request.ContentLength > 0 || Request.Headers.TransferEncoding.Count > 0)
        {
            // Giữ nguyên Content-Type (kể cả boundary của multipart) để backend đọc được file giáo trình.
            request.Content = new StreamContent(Request.Body);
            if (Request.ContentType is { } contentType)
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        }

        try
        {
            using var response = await clients.CreateClient("Attendance").SendAsync(request, ct);
            Response.Headers.CacheControl = "no-store";
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json; charset=utf-8",
                Content = await response.Content.ReadAsStringAsync(ct)
            };
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(503, new { message = "Không thể kết nối dịch vụ quản lý đào tạo. Vui lòng thử lại." });
        }
    }
}
