using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Controllers;

/// <summary>
/// Chuyển tiếp ảnh thẻ (ảnh chụp lúc nhập học) từ backend, dùng làm ảnh đại diện.
/// Trình duyệt chỉ gọi website nên token đăng nhập không lộ ra phía client.
/// </summary>
[Authorize]
public sealed class IdentityPhotoController(IHttpClientFactory clients) : Controller
{
    [HttpGet("anh-dai-dien")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        // Chỉ sinh viên có ảnh thẻ; ai chưa có ảnh thẻ thì dùng ảnh mẫu tạm.
        if (User.IsInRole(RoleNames.SinhVien) && await Forward("api/v1/anh-the/cua-toi", ct) is FileContentResult photo)
            return photo;
        Response.Headers.CacheControl = "private, max-age=300";
        return Redirect(User.AnhMau());
    }

    [HttpGet("quan-ly-nguoi-dung/anh-the/{code}"), Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public Task<IActionResult> Student(string code, CancellationToken ct) =>
        Forward($"api/v1/anh-the/sinh-vien/{Uri.EscapeDataString(code)}", ct);

    /// <summary>Học vụ tải ảnh thẻ từ hồ sơ nhập học cho sinh viên ở trang chi tiết tài khoản.</summary>
    [HttpPost("quan-ly-nguoi-dung/anh-the/{code}"), Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    [ValidateAntiForgeryToken, RequestSizeLimit(1_200_000)]
    public async Task<IActionResult> Upload(string code, Guid id, IFormFile? anhThe, CancellationToken ct)
    {
        if (anhThe is null || anhThe.Length == 0 || anhThe.Length > 1_050_000
            || anhThe.ContentType is not ("image/jpeg" or "image/png"))
        {
            TempData["Error"] = "Chọn ảnh thẻ JPEG/PNG tối đa 1 MB.";
            return RedirectToAction("Details", "UserManagement", new { id });
        }
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Account");
        using var buffer = new MemoryStream();
        await anhThe.CopyToAsync(buffer, ct);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/v1/qr-diem-danh/ho-so/{Uri.EscapeDataString(code)}/anh-the");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(new { frame = Convert.ToBase64String(buffer.ToArray()) }),
            Encoding.UTF8, "application/json");
        try
        {
            using var response = await clients.CreateClient("Attendance").SendAsync(request, ct);
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
                ? "Đã cập nhật ảnh thẻ của sinh viên."
                : "Không lưu được ảnh thẻ. Hãy dùng ảnh chân dung JPEG/PNG rõ mặt.";
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            TempData["Error"] = "Không thể kết nối máy chủ. Vui lòng thử lại.";
        }
        return RedirectToAction("Details", "UserManagement", new { id });
    }

    private async Task<IActionResult> Forward(string path, CancellationToken ct)
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token)) return NotFound();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var response = await clients.CreateClient("Attendance").SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return NotFound();
            Response.Headers.CacheControl = "private, max-age=300";
            return File(await response.Content.ReadAsByteArrayAsync(ct), "image/jpeg");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return NotFound();
        }
    }
}
