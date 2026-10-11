using System.Security.Claims;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers;

[Route("tai-khoan")]
public sealed class AccountController(IAuthApiClient authApi, IFaceRecognitionApiClient faces) : Controller
{
    public const string MustChangePasswordClaim = "must_change_password";
    private const string PendingFaceAccessToken = "PendingFaceAccessToken";
    private const string PendingFaceRefreshToken = "PendingFaceRefreshToken";
    private const string PendingFaceUserName = "PendingFaceUserName";
    private const string PendingFaceExpiresAt = "PendingFaceExpiresAt";
    private const string PendingFaceStartedAt = "PendingFaceStartedAt";
    private const string PendingFaceRememberMe = "PendingFaceRememberMe";
    private const string PendingFaceReturnUrl = "PendingFaceReturnUrl";

    [AllowAnonymous, HttpGet("dang-nhap")]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("AccessToken")))
                return RedirectToAction("Index", "Home");
            await SignOutLocalAsync();
        }
        ClearPendingFace();
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

        if (!auth.MustChangePassword && auth.Roles.Contains(RoleNames.SinhVien))
        {
            ApiResult<FaceProfileMetadataModel> profile;
            try { profile = await faces.GetMyProfileAsync(auth.AccessToken, ct); }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không thể kiểm tra hồ sơ khuôn mặt. Hãy thử lại.");
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Backend phản hồi quá chậm. Hãy thử lại.");
                return View(model);
            }
            if (!profile.Succeeded || profile.Value is null)
            {
                ModelState.AddModelError(string.Empty, profile.Error ?? "Không thể kiểm tra hồ sơ khuôn mặt.");
                return View(model);
            }
            if (profile.Value.DaDangKy)
            {
                ClearPendingFace();
                HttpContext.Session.SetString(PendingFaceAccessToken, auth.AccessToken);
                HttpContext.Session.SetString(PendingFaceRefreshToken, auth.RefreshToken);
                HttpContext.Session.SetString(PendingFaceUserName, auth.UserName);
                HttpContext.Session.SetString(PendingFaceExpiresAt, auth.AccessTokenExpiresAtUtc.ToString("O"));
                HttpContext.Session.SetString(PendingFaceStartedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                HttpContext.Session.SetString(PendingFaceRememberMe, model.RememberMe ? "1" : "0");
                HttpContext.Session.SetString(PendingFaceReturnUrl, Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
                return RedirectToAction(nameof(VerifyFace));
            }
        }

        await SignInLocalAsync(auth, model.RememberMe);
        if (auth.MustChangePassword)
        {
            TempData["PasswordChangeRequired"] = "Đây là lần đăng nhập đầu tiên. Vui lòng đổi mật khẩu tạm trước khi tiếp tục.";
            return RedirectToAction(nameof(ChangePassword));
        }
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }

    [AllowAnonymous, HttpGet("xac-thuc-khuon-mat")]
    public IActionResult VerifyFace()
    {
        if (!HasValidPendingFace())
        {
            ClearPendingFace();
            TempData["Error"] = "Phiên xác thực khuôn mặt đã hết hạn. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login));
        }
        return View(new FaceVerificationViewModel
        {
            MaDangNhap = HttpContext.Session.GetString(PendingFaceUserName) ?? string.Empty
        });
    }

    [AllowAnonymous, HttpPost("xac-thuc-khuon-mat"), ValidateAntiForgeryToken]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> VerifyFace(FaceVerificationViewModel model, CancellationToken ct)
    {
        if (!HasValidPendingFace())
        {
            ClearPendingFace();
            TempData["Error"] = "Phiên xác thực khuôn mặt đã hết hạn. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login));
        }
        model.MaDangNhap = HttpContext.Session.GetString(PendingFaceUserName) ?? string.Empty;
        if (!TryReadDescriptors(model.DescriptorsJson, out var descriptors))
        {
            return FaceVerificationError(model, "Dữ liệu khuôn mặt không hợp lệ. Hãy chụp lại.");
        }

        var token = HttpContext.Session.GetString(PendingFaceAccessToken)!;
        ApiResult result;
        try { result = await faces.VerifyLoginFaceAsync(token, descriptors, ct); }
        catch (HttpRequestException)
        {
            return FaceVerificationError(model, "Không thể kết nối đến backend API.");
        }
        catch (TaskCanceledException)
        {
            return FaceVerificationError(model, "Backend phản hồi quá chậm. Hãy thử lại.");
        }
        if (!result.Succeeded)
        {
            return FaceVerificationError(model, result.Error ?? "Khuôn mặt không khớp. Hãy thử lại.");
        }

        var auth = new AuthApiResponse(
            token,
            HttpContext.Session.GetString(PendingFaceRefreshToken)!,
            DateTimeOffset.Parse(HttpContext.Session.GetString(PendingFaceExpiresAt)!, CultureInfo.InvariantCulture).UtcDateTime,
            model.MaDangNhap,
            [RoleNames.SinhVien],
            false);
        var rememberMe = HttpContext.Session.GetString(PendingFaceRememberMe) == "1";
        var returnUrl = HttpContext.Session.GetString(PendingFaceReturnUrl);
        ClearPendingFace();
        await SignInLocalAsync(auth, rememberMe);
        TempData["Success"] = "Xác thực khuôn mặt thành công. Bạn đã đăng nhập.";
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }

    private IActionResult FaceVerificationError(FaceVerificationViewModel model, string message)
    {
        model.DescriptorsJson = string.Empty;
        ModelState.Remove(nameof(FaceVerificationViewModel.DescriptorsJson));
        ModelState.AddModelError(string.Empty, message);
        ViewData["AutoStartFace"] = false;
        return View(nameof(VerifyFace), model);
    }

    private async Task SignInLocalAsync(AuthApiResponse auth, bool rememberMe)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, auth.UserName) };
        claims.AddRange(auth.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.Add(new Claim(MustChangePasswordClaim, auth.MustChangePassword.ToString().ToLowerInvariant()));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = rememberMe, ExpiresUtc = auth.AccessTokenExpiresAtUtc });
        HttpContext.Session.SetString("AccessToken", auth.AccessToken);
        HttpContext.Session.SetString("RefreshToken", auth.RefreshToken);
    }

    private bool HasValidPendingFace()
    {
        var session = HttpContext.Session;
        return !string.IsNullOrWhiteSpace(session.GetString(PendingFaceAccessToken))
            && !string.IsNullOrWhiteSpace(session.GetString(PendingFaceRefreshToken))
            && !string.IsNullOrWhiteSpace(session.GetString(PendingFaceUserName))
            && DateTimeOffset.TryParse(session.GetString(PendingFaceExpiresAt), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var expiresAt)
            && expiresAt > DateTimeOffset.UtcNow
            && long.TryParse(session.GetString(PendingFaceStartedAt), out var startedAt)
            && startedAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - startedAt < 5 * 60;
    }

    private void ClearPendingFace()
    {
        var session = HttpContext.Session;
        foreach (var key in new[] { PendingFaceAccessToken, PendingFaceRefreshToken, PendingFaceUserName,
                     PendingFaceExpiresAt, PendingFaceStartedAt, PendingFaceRememberMe, PendingFaceReturnUrl })
            session.Remove(key);
    }

    private static bool TryReadDescriptors(string? json, out IReadOnlyCollection<double[]> descriptors)
    {
        descriptors = [];
        try
        {
            var value = JsonSerializer.Deserialize<double[][]>(json ?? string.Empty);
            if (value is null || value.Length is < 3 or > 5
                || value.Any(x => x is null || x.Length != 512 || x.Any(y => !double.IsFinite(y))))
                return false;
            descriptors = value;
            return true;
        }
        catch (JsonException) { return false; }
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
