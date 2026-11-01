using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers;

[Authorize]
[Route("thong-tin-ca-nhan")]
public sealed class PersonalProfileController(IUserManagementApiClient api) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (token is null) return RedirectToAction("Login", "Account");
        try
        {
            var result = await api.GetMeAsync(token, ct);
            if (!result.Succeeded || result.Value is null)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction("Index", "Home");
            }
            return View(result.Value);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Không thể kết nối đến backend API.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet("cap-nhat")]
    public async Task<IActionResult> Edit(CancellationToken ct)
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (token is null) return RedirectToAction("Login", "Account");
        var result = await api.GetMeAsync(token, ct);
        if (!result.Succeeded || result.Value is null) return RedirectToAction(nameof(Index));
        return View(new PersonalProfileEditViewModel
        {
            UserName = result.Value.UserName,
            FullName = result.Value.FullName,
            Email = result.Value.Email,
            PhoneNumber = result.Value.PhoneNumber,
            ProfileType = result.Value.ProfileType,
            AnhDaiDien = result.Value.AcademicProfile?.AnhDaiDien,
            GioiTinh = result.Value.AcademicProfile?.GioiTinh,
            NgaySinh = result.Value.AcademicProfile?.NgaySinh,
            NoiSinh = result.Value.AcademicProfile?.NoiSinh,
            DanToc = result.Value.AcademicProfile?.DanToc,
            TonGiao = result.Value.AcademicProfile?.TonGiao,
            QuocTich = result.Value.AcademicProfile?.QuocTich,
            DiaChiLienHe = result.Value.AcademicProfile?.DiaChiLienHe,
            HoKhauThuongTru = result.Value.AcademicProfile?.HoKhauThuongTru
        });
    }

    [HttpPost("cap-nhat"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PersonalProfileEditViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var token = HttpContext.Session.GetString("AccessToken");
        if (token is null) return RedirectToAction("Login", "Account");
        try
        {
            var result = await api.UpdateMeAsync(token, model, ct);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật thông tin.");
                return View(model);
            }
            TempData["Success"] = "Đã cập nhật thông tin cá nhân.";
            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến backend API.");
            return View(model);
        }
    }
}
