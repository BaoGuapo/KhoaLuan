using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers;

[Authorize(Roles = RoleNames.GiangVien)]
public sealed class GiangVienController(ITeachingManagementApiClient api) : Controller
{
    public async Task<IActionResult> LichGiangDay(CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetMyAssignmentsAsync(token, ct));
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return View(Array.Empty<TeachingAssignmentModel>());
        }
        return View(result.Value ?? []);
    }

    public async Task<IActionResult> QuanLyLop(string maLop, string? maBuoiHoc, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetClassWorkspaceAsync(token, maLop, maBuoiHoc, ct));
        if (!result.Succeeded || result.Value is null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(LichGiangDay));
        }
        return View(result.Value);
    }

    public async Task<IActionResult> LichSuDiemDanh(string maLop, string? maBuoiHoc, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var history = await SafeAsync(() => api.GetClassAttendanceHistoryAsync(token, maLop, ct));
        if (!history.Succeeded || history.Value is null)
        {
            TempData["Error"] = history.Error;
            return RedirectToAction(nameof(LichGiangDay));
        }

        // Chi tiết một buổi dùng lại dữ liệu workspace, chỉ để xem nên không cần kiểm tra khung giờ.
        LecturerClassWorkspaceModel? detail = null;
        if (!string.IsNullOrWhiteSpace(maBuoiHoc))
        {
            var result = await SafeAsync(() => api.GetClassWorkspaceAsync(token, maLop, maBuoiHoc, ct));
            if (result.Succeeded) detail = result.Value;
            else TempData["Error"] = result.Error;
        }

        return View(new ClassAttendanceHistoryPageViewModel { LichSu = history.Value, ChiTietBuoi = detail });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatDiemDanh(
        string maLop,
        string maBuoiHoc,
        string maSinhVien,
        string trangThai,
        CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.UpdateAttendanceAsync(
            token, maBuoiHoc, maSinhVien, trangThai, ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Đã cập nhật trạng thái điểm danh."
            : result.Error;
        return RedirectToAction(nameof(QuanLyLop), new { maLop, maBuoiHoc });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChotDiemDanh(string maLop, string maBuoiHoc, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.FinalizeAttendanceAsync(token, maBuoiHoc, ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Đã chốt điểm danh và cập nhật tổng hợp chuyên cần."
            : result.Error;
        return RedirectToAction(nameof(QuanLyLop), new { maLop, maBuoiHoc });
    }

    private string? AccessToken => HttpContext.Session.GetString("AccessToken");
    private IActionResult LoginRedirect() => RedirectToAction("Login", "Account");

    private static async Task<ApiResult<T>> SafeAsync<T>(Func<Task<ApiResult<T>>> action)
    {
        try { return await action(); }
        catch (HttpRequestException) { return new ApiResult<T>(false, default, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new ApiResult<T>(false, default, "Backend API phản hồi quá thời gian."); }
    }

    private static async Task<ApiResult> SafeAsync(Func<Task<ApiResult>> action)
    {
        try { return await action(); }
        catch (HttpRequestException) { return new ApiResult(false, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new ApiResult(false, "Backend API phản hồi quá thời gian."); }
    }
}
