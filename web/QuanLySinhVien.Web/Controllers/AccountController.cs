using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers;

[Route("tai-khoan")]
public sealed class AccountController(IAuthApiClient authApi) : Controller
{
    [AllowAnonymous, HttpGet("dang-nhap")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous, HttpPost("dang-nhap"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, CancellationToken ct)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        AuthApiResponse? auth;
        try { auth = await authApi.LoginAsync(model, ct); }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến backend API.");
            return View(model);
        }
        if (auth is null)
        {
            ModelState.AddModelError(string.Empty, "Mã sinh viên, mã giảng viên hoặc mật khẩu không đúng.");
            return View(model);
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, auth.UserName) };
        claims.AddRange(auth.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe, ExpiresUtc = auth.AccessTokenExpiresAtUtc });
        HttpContext.Session.SetString("AccessToken", auth.AccessToken);
        HttpContext.Session.SetString("RefreshToken", auth.RefreshToken);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }

    [Authorize, HttpGet("doi-mat-khau")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize, HttpPost("doi-mat-khau"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction(nameof(Login));
        if (!await authApi.ChangePasswordAsync(token, model, ct))
        {
            ModelState.AddModelError(string.Empty, "Mật khẩu hiện tại không đúng hoặc phiên đăng nhập đã hết hạn.");
            return View(model);
        }
        TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
        await SignOutLocalAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpPost("dang-xuat"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var accessToken = HttpContext.Session.GetString("AccessToken");
        var refreshToken = HttpContext.Session.GetString("RefreshToken");
        if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            try { await authApi.LogoutAsync(accessToken, refreshToken, ct); } catch (HttpRequestException) { }
        }
        await SignOutLocalAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet("tu-choi-truy-cap")]
    public IActionResult AccessDenied() => View();

    private async Task SignOutLocalAsync()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
