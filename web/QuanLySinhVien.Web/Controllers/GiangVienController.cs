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
        // Mở trang mà chưa chọn buổi thì hiện luôn buổi gần nhất đã diễn ra, khỏi phải bấm thêm.
        maBuoiHoc ??= history.Value.BuoiHoc
            .Where(x => x.NgayHocCuThe.Date <= DateTime.Today)
            .OrderByDescending(x => x.NgayHocCuThe)
            .FirstOrDefault()?.MaBuoiHoc;
        IReadOnlyCollection<AttendanceCorrectionModel> corrections = [];
        if (!string.IsNullOrWhiteSpace(maBuoiHoc))
        {
            var result = await SafeAsync(() => api.GetClassWorkspaceAsync(token, maLop, maBuoiHoc, ct));
            if (result.Succeeded) detail = result.Value;
            else TempData["Error"] = result.Error;
            var log = await SafeAsync(() => api.GetCorrectionsAsync(token, maBuoiHoc, ct));
            if (log.Succeeded && log.Value is not null) corrections = log.Value;
        }

        return View(new ClassAttendanceHistoryPageViewModel { LichSu = history.Value, ChiTietBuoi = detail, LichSuSua = corrections });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiLaiCanhBao(
        string maLop,
        string maSinhVien,
        string? maBuoiHoc,
        CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.ResendAttendanceWarningAsync(token, maLop, maSinhVien, ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"Đã gửi lại thư cảnh báo chuyên cần cho {maSinhVien}."
            : result.Error;
        return RedirectToAction(nameof(QuanLyLop), new { maLop, maBuoiHoc });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DieuChinhDiemDanh(
        string maLop,
        string maBuoiHoc,
        string maSinhVien,
        string trangThai,
        string? lyDo,
        CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length < 5)
        {
            TempData["Error"] = "Cần ghi lý do sửa (ít nhất 5 ký tự).";
            return RedirectToAction(nameof(LichSuDiemDanh), new { maLop, maBuoiHoc });
        }
        var result = await SafeAsync(() => api.CorrectAttendanceAsync(
            token, maBuoiHoc, maSinhVien, trangThai, lyDo.Trim(), ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"Đã sửa điểm danh của {maSinhVien} và lưu lý do."
            : result.Error;
        return RedirectToAction(nameof(LichSuDiemDanh), new { maLop, maBuoiHoc });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatDiemDanh(
        string maLop,
        string maBuoiHoc,
        string maSinhVien,
        string trangThai,
        string? viTri,
        CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.UpdateAttendanceAsync(
            token, maBuoiHoc, maSinhVien, trangThai, ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Đã cập nhật trạng thái điểm danh."
            : result.Error;
        // Giữ bộ lọc "ngoài khuôn viên" để giảng viên xử lý tiếp các sinh viên còn lại.
        return RedirectToAction(nameof(QuanLyLop), new { maLop, maBuoiHoc, viTri = viTri == "ngoai" ? viTri : null });
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

    public async Task<IActionResult> NhapDiem(string maLop, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetGradeSheetAsync(token, maLop, ct));
        if (!result.Succeeded || result.Value is null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(LichGiangDay));
        }
        return View(result.Value);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuDiem(string maLop, List<string> maSinhVien, List<string?> diem, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var grades = new List<(string, decimal?)>();
        for (var i = 0; i < maSinhVien.Count; i++)
        {
            var text = i < diem.Count ? diem[i]?.Trim().Replace(',', '.') : null;
            if (string.IsNullOrEmpty(text)) { grades.Add((maSinhVien[i], null)); continue; }
            if (!decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                TempData["Error"] = $"Điểm của sinh viên {maSinhVien[i]} không phải là số.";
                return RedirectToAction(nameof(NhapDiem), new { maLop });
            }
            grades.Add((maSinhVien[i], value));
        }
        var result = await SafeAsync(() => api.SaveGradesAsync(token, maLop, grades, ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Đã lưu điểm nháp. Sinh viên chưa thấy điểm cho tới khi công bố."
            : result.Error;
        return RedirectToAction(nameof(NhapDiem), new { maLop });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CongBoDiem(string maLop, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.PublishGradesAsync(token, maLop, ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Đã công bố điểm. Sinh viên đã xem được kết quả."
            : result.Error;
        return RedirectToAction(nameof(NhapDiem), new { maLop });
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
