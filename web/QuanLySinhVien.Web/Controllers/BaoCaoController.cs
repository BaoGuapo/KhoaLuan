using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers;

[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
[Route("bao-cao")]
public sealed class BaoCaoController(AttendanceReportApiClient api) : Controller
{
    private string? Token => HttpContext.Session.GetString("AccessToken");

    [HttpGet("")]
    public async Task<IActionResult> Index(string? maHocKy, CancellationToken ct)
    {
        if (Token is not { } token) return RedirectToAction("Login", "Account");

        var semesters = await api.SemestersAsync(token, ct);
        if (!semesters.Succeeded || semesters.Value is null)
        {
            TempData["Error"] = semesters.Error;
            return View(new AttendanceReportPageViewModel { HocKy = [] });
        }

        // Chưa chọn học kỳ thì mở học kỳ mới nhất, vì API trả danh sách đã sắp theo ngày bắt đầu giảm dần.
        var selected = maHocKy is { Length: > 0 } && semesters.Value.Any(x => x.MaHocKy == maHocKy)
            ? maHocKy
            : semesters.Value.FirstOrDefault()?.MaHocKy;
        if (selected is null)
            return View(new AttendanceReportPageViewModel { HocKy = semesters.Value });

        var report = await api.ReportAsync(token, selected, ct);
        if (!report.Succeeded) TempData["Error"] = report.Error;
        return View(new AttendanceReportPageViewModel
        {
            HocKy = semesters.Value,
            MaHocKyDangChon = selected,
            BaoCao = report.Value
        });
    }

    [HttpGet("xuat-excel")]
    public async Task<IActionResult> XuatExcel(string maHocKy, CancellationToken ct)
    {
        if (Token is not { } token) return RedirectToAction("Login", "Account");
        var result = await api.ExcelAsync(token, maHocKy, ct);
        if (!result.Succeeded || result.Value is null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index), new { maHocKy });
        }
        return File(result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"bao-cao-diem-danh-{maHocKy}.xlsx");
    }
}
