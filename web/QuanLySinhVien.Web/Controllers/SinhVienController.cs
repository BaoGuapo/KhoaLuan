using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers
{
    [Authorize(Roles = "SinhVien")]
    public class SinhVienController(StudentAcademicApiClient api) : Controller
    {
        public async Task<IActionResult> DangKyHocPhan(string? hocKy, string? q, string? mon, bool conCho, CancellationToken ct)
        {
            if (HttpContext.Session.GetString("AccessToken") is not { } token) return RedirectToAction("Login", "Account");
            var model = await api.LoadAsync(token, ct);
            SelectSemester(model, hocKy);
            model.Query = q?.Trim(); model.Subject = mon; model.OnlyAvailable = conCho;
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(string maLop, string? hocKy, string? mon, string? q, bool conCho, CancellationToken ct)
        {
            if (HttpContext.Session.GetString("AccessToken") is not { } token) return RedirectToAction("Login", "Account");
            if (string.IsNullOrWhiteSpace(maLop) || maLop.Length > 50) TempData["Error"] = "Vui lòng chọn lớp học phần hợp lệ.";
            else
            {
                var result = await api.RegisterAsync(token, maLop, ct);
                TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? $"Đã đăng ký lớp {maLop}. Bạn có thể xem kết quả ở cuối trang." : result.Error;
            }
            return RedirectToAction(nameof(DangKyHocPhan), new { hocKy, mon, q, conCho });
        }

        public async Task<IActionResult> XemLichHoc(string? hocKy, DateTime? tuan, CancellationToken ct)
        {
            if (HttpContext.Session.GetString("AccessToken") is not { } token) return RedirectToAction("Login", "Account");
            var model = await api.LoadAsync(token, ct, includeOffers: false); SelectSemester(model, hocKy);
            var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time").Date;
            var date = tuan is { Year: >= 2000 and <= 2100 } ? tuan.Value.Date : today;
            model.WeekStart = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
            return View(model);
        }

        public static void SelectSemester(StudentAcademicWorkspace model, string? selected)
        {
            var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time").Date;
            model.Semesters = model.Semesters.OrderByDescending(x => x.NgayBatDau).ToList();
            model.Semester = model.Semesters.Any(x => x.MaHocKy == selected) ? selected
                : model.Semesters.FirstOrDefault(x => x.NgayBatDau <= now && x.NgayKetThuc >= now && model.Available.Concat(model.Registered).Any(c => c.MaHocKy == x.MaHocKy))?.MaHocKy
                ?? model.Semesters.FirstOrDefault(x => model.Available.Concat(model.Registered).Any(c => c.MaHocKy == x.MaHocKy))?.MaHocKy
                ?? model.Semesters.FirstOrDefault()?.MaHocKy;
        }
    }
}
