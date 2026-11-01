using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Controllers;

[Authorize]
public sealed class HomeController : Controller
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
    public IActionResult SinhVien() => View();

    [AllowAnonymous]
    public IActionResult Error() => View();
}
