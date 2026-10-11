using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers;

[Authorize]
public sealed class HomeController(IUserManagementApiClient userApi) : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole(RoleNames.Admin)) return RedirectToAction(nameof(Admin));
        if (User.IsInRole(RoleNames.CanBoQuanLy)) return RedirectToAction(nameof(CanBoQuanLy));
        if (User.IsInRole(RoleNames.GiangVien)) return RedirectToAction(nameof(GiangVien));
        if (User.IsInRole(RoleNames.SinhVien)) return RedirectToAction(nameof(SinhVien));
        return RedirectToAction("AccessDenied", "Account");
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult Admin() => View();

    [Authorize(Policy = "CanBoQuanLyOnly")]
    public IActionResult CanBoQuanLy() => View();

    [Authorize(Policy = "GiangVienOnly")]
    public IActionResult GiangVien() => View();

    [Authorize(Policy = "SinhVienOnly")]
    public async Task<IActionResult> SinhVien(CancellationToken ct)
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Account");

        try
        {
            var result = await userApi.GetMeAsync(token, ct);
            if (result.Succeeded && result.Value is not null) return View(result.Value);
        }
        catch (HttpRequestException)
        {
            // The dashboard remains useful through its navigation links when the API is unavailable.
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // An API timeout should not make the student's home page unavailable.
        }

        ViewData["ProfileUnavailable"] = true;
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
