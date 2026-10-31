using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers;

[Route("khuon-mat")]
public sealed class FaceRecognitionController(IFaceRecognitionApiClient api) : Controller
{
    [HttpGet("")]
    [Authorize(Roles = RoleNames.SinhVien)]
    public IActionResult Profile() => RedirectToAction("Profile", "QrAttendance");
    [HttpPost("luu"), ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> Save(string descriptorsJson, bool replace, CancellationToken ct)
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token)) return LoginRedirect();
        if (!TryReadDescriptors(descriptorsJson, out var descriptors, out var error))
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Profile));
        }

        var result = await SafeAsync(() => api.SaveMyProfileAsync(token, descriptors, replace, ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? replace ? "Đã cập nhật hồ sơ khuôn mặt." : "Đăng ký khuôn mặt thành công."
            : result.Error ?? "Không thể lưu hồ sơ khuôn mặt.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet("diem-danh/{maBuoiHoc}")]
    [Authorize(Roles = RoleNames.GiangVien)]
    public IActionResult Attendance(string maBuoiHoc, string maLop, string? ngayHoc) =>
        RedirectToAction("Index", "QrAttendance", new { maBuoiHoc });

    [HttpPost("diem-danh/{maBuoiHoc}"), ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.GiangVien)]
    public async Task<IActionResult> Attendance(
        string maBuoiHoc,
        FaceAttendanceCaptureViewModel model,
        CancellationToken ct)
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token)) return LoginRedirect();
        if (!TryReadDescriptors(model.DescriptorsJson, out var descriptors, out var error))
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Attendance), new { maBuoiHoc, maLop = model.MaLopHocPhan, ngayHoc = model.NgayHoc });
        }

        var result = await SafeAsync(() => api.RecognizeAttendanceAsync(token, maBuoiHoc, descriptors, ct));
        if (result.Succeeded && result.Value is not null)
        {
            TempData["Success"] = result.Value.DaDiemDanhTruocDo
                ? $"{result.Value.MaSinhVien} - {result.Value.HoTen} đã được điểm danh trước đó."
                : $"Đã nhận diện và điểm danh {result.Value.MaSinhVien} - {result.Value.HoTen}.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Không thể nhận diện khuôn mặt.";
        }
        return RedirectToAction(nameof(Attendance), new { maBuoiHoc, maLop = model.MaLopHocPhan, ngayHoc = model.NgayHoc });
    }

    private static bool TryReadDescriptors(
        string? json,
        out IReadOnlyCollection<double[]> descriptors,
        out string error)
    {
        descriptors = [];
        error = string.Empty;
        try
        {
            var value = JsonSerializer.Deserialize<double[][]>(json ?? string.Empty);
            if (value is null || value.Length is < 3 or > 5 || value.Any(x => x is null || x.Length != 512))
            {
                error = "Dữ liệu khuôn mặt không hợp lệ. Hãy bật camera và chụp lại.";
                return false;
            }
            descriptors = value;
            return true;
        }
        catch (JsonException)
        {
            error = "Không đọc được dữ liệu khuôn mặt. Hãy chụp lại.";
            return false;
        }
    }

    private IActionResult LoginRedirect() => RedirectToAction("Login", "Account");

    private static async Task<ApiResult<T>> SafeAsync<T>(Func<Task<ApiResult<T>>> action)
    {
        try { return await action(); }
        catch (HttpRequestException) { return new ApiResult<T>(false, default, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new ApiResult<T>(false, default, "Backend phản hồi quá chậm."); }
    }

    private static async Task<ApiResult> SafeAsync(Func<Task<ApiResult>> action)
    {
        try { return await action(); }
        catch (HttpRequestException) { return new ApiResult(false, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new ApiResult(false, "Backend phản hồi quá chậm."); }
    }
}
