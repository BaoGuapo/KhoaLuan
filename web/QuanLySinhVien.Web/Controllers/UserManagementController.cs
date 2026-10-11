using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers;

[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
[Route("quan-ly-nguoi-dung")]
public sealed class UserManagementController(
    IUserManagementApiClient api,
    ITeachingManagementApiClient teachingApi) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? role, bool? isActive, int page = 1, CancellationToken ct = default)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetUsersAsync(token, search, role, isActive, page, ct));
        if (!result.Succeeded || result.Value is null)
        {
            TempData["Error"] = result.Error;
            result = new ApiResult<PagedApiResponse<UserSummaryModel>>(true,
                new PagedApiResponse<UserSummaryModel>([], 1, 20, 0, 0));
        }
        return View(new UserListViewModel
        {
            Result = result.Value!,
            Search = search,
            Role = role,
            IsActive = isActive
        });
    }

    [HttpGet("chi-tiet/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetUserAsync(token, id, ct));
        if (!result.Succeeded || result.Value is null) return ApiFailure(result.Error);
        return View(result.Value);
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet("tao-tai-khoan-noi-bo")]
    public IActionResult CreateStaff() => View(new CreateStaffAccountViewModel());

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost("tao-tai-khoan-noi-bo"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff(CreateStaffAccountViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.CreateStaffAsync(token, model, ct));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể tạo tài khoản.");
            return View(model);
        }
        TempData["Success"] = "Đã tạo tài khoản. Người dùng phải đổi mật khẩu tạm ở lần đăng nhập đầu tiên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/trang-thai"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(Guid id, bool isActive, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.SetStatusAsync(token, id, isActive, ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? isActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản và thu hồi các phiên đăng nhập."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/dat-lai-mat-khau")]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var user = await SafeAsync(() => api.GetUserAsync(token, id, ct));
        if (!user.Succeeded || user.Value is null) return ApiFailure(user.Error);
        return View(new ResetPasswordViewModel { UserId = id, UserName = user.Value.UserName });
    }

    [HttpPost("{id:guid}/dat-lai-mat-khau"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordViewModel model, CancellationToken ct)
    {
        model.UserId = id;
        if (!ModelState.IsValid) return View(model);
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.ResetPasswordAsync(token, id, model.TemporaryPassword, ct));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể đặt lại mật khẩu.");
            return View(model);
        }
        TempData["Success"] = "Đã cấp mật khẩu tạm mới. Người dùng phải đổi mật khẩu ở lần đăng nhập tiếp theo.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost("{id:guid}/vai-tro"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(Guid id, string role, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.ChangeRoleAsync(token, id, role, ct));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Đã cập nhật vai trò. Người dùng phải đăng nhập lại."
            : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("sinh-vien")]
    public async Task<IActionResult> Students(string? search, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetStudentsAsync(token, search, ct));
        ViewData["Search"] = search;
        if (!result.Succeeded) TempData["Error"] = result.Error;
        return View(result.Value ?? []);
    }

    [HttpGet("sinh-vien/tao")]
    public IActionResult CreateStudent() => View(new SinhVienFormViewModel());

    [HttpPost("sinh-vien/tao"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStudent(SinhVienFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.CreateStudentAsync(token, model, ct));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể tạo hồ sơ sinh viên.");
            return View(model);
        }
        TempData["Success"] = "Đã tạo hồ sơ sinh viên. Bạn có thể cấp tài khoản đăng nhập ở bước tiếp theo.";
        return RedirectToAction(nameof(Students));
    }

    [HttpGet("sinh-vien/{code}/sua")]
    public async Task<IActionResult> EditStudent(string code, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetStudentAsync(token, code, ct));
        if (!result.Succeeded || result.Value is null) return ApiFailure(result.Error);
        return View(ToStudentForm(result.Value));
    }

    [HttpPost("sinh-vien/{code}/sua"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStudent(string code, SinhVienFormViewModel model, CancellationToken ct)
    {
        model.MaSinhVien = code;
        if (!ModelState.IsValid) return View(model);
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.UpdateStudentAsync(token, code, model, ct));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật hồ sơ.");
            return View(model);
        }
        TempData["Success"] = "Đã cập nhật hồ sơ sinh viên và thông tin tài khoản liên kết.";
        return RedirectToAction(nameof(Students));
    }

    [HttpGet("sinh-vien/{code}/cap-tai-khoan")]
    public async Task<IActionResult> ProvisionStudent(string code, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetStudentAsync(token, code, ct));
        if (!result.Succeeded || result.Value is null) return ApiFailure(result.Error);
        return View("ProvisionAccount", new ProvisionAccountViewModel
        {
            ProfileCode = code,
            FullName = result.Value.HoTen,
            Email = result.Value.Email,
            IsLecturer = false
        });
    }

    [HttpPost("sinh-vien/{code}/cap-tai-khoan"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ProvisionStudent(string code, ProvisionAccountViewModel model, CancellationToken ct)
    {
        model.ProfileCode = code;
        model.IsLecturer = false;
        if (!ModelState.IsValid) return View("ProvisionAccount", model);
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.ProvisionStudentAsync(token, code, model.InitialPassword, ct));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cấp tài khoản.");
            return View("ProvisionAccount", model);
        }
        TempData["Success"] = "Đã cấp tài khoản sinh viên với mật khẩu tạm.";
        return RedirectToAction(nameof(Students));
    }

    [HttpGet("giang-vien")]
    public async Task<IActionResult> Lecturers(
        string? search,
        string? khoa,
        string? chuyenNganh,
        CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetLecturersAsync(token, search, khoa, chuyenNganh, ct));
        ViewData["Search"] = search;
        ViewData["Khoa"] = khoa;
        ViewData["ChuyenNganh"] = chuyenNganh;
        if (!result.Succeeded) TempData["Error"] = result.Error;
        return View(result.Value ?? []);
    }

    [HttpGet("giang-vien/tao")]
    public IActionResult CreateLecturer() => View(new GiangVienFormViewModel());

    [HttpPost("giang-vien/tao"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLecturer(GiangVienFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.CreateLecturerAsync(token, model, ct));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể tạo hồ sơ giảng viên.");
            return View(model);
        }
        TempData["Success"] = "Đã tạo hồ sơ giảng viên. Bạn có thể cấp tài khoản đăng nhập ở bước tiếp theo.";
        return RedirectToAction(nameof(Lecturers));
    }

    [HttpGet("giang-vien/{code}/sua")]
    public async Task<IActionResult> EditLecturer(string code, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetLecturerAsync(token, code, ct));
        if (!result.Succeeded || result.Value is null) return ApiFailure(result.Error);
        return View(ToLecturerForm(result.Value));
    }

    [HttpPost("giang-vien/{code}/sua"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLecturer(string code, GiangVienFormViewModel model, CancellationToken ct)
    {
        model.MaGiangVien = code;
        if (!ModelState.IsValid) return View(model);
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.UpdateLecturerAsync(token, code, model, ct));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật hồ sơ.");
            return View(model);
        }
        TempData["Success"] = "Đã cập nhật hồ sơ giảng viên và thông tin tài khoản liên kết.";
        return RedirectToAction(nameof(Lecturers));
    }

    [HttpGet("giang-vien/{code}/cap-tai-khoan")]
    public async Task<IActionResult> ProvisionLecturer(string code, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.GetLecturerAsync(token, code, ct));
        if (!result.Succeeded || result.Value is null) return ApiFailure(result.Error);
        return View("ProvisionAccount", new ProvisionAccountViewModel
        {
            ProfileCode = code,
            FullName = result.Value.HoTen,
            Email = result.Value.Email,
            IsLecturer = true
        });
    }

    [HttpPost("giang-vien/{code}/cap-tai-khoan"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ProvisionLecturer(string code, ProvisionAccountViewModel model, CancellationToken ct)
    {
        model.ProfileCode = code;
        model.IsLecturer = true;
        if (string.IsNullOrWhiteSpace(model.Email)) ModelState.AddModelError(nameof(model.Email), "Vui lòng nhập email.");
        if (!ModelState.IsValid) return View("ProvisionAccount", model);
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => api.ProvisionLecturerAsync(token, code, model.Email!, model.InitialPassword, ct));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cấp tài khoản.");
            return View("ProvisionAccount", model);
        }
        TempData["Success"] = "Đã cấp tài khoản giảng viên với mật khẩu tạm.";
        return RedirectToAction(nameof(Lecturers));
    }

    [HttpGet("giang-vien/{code}/phan-cong")]
    public async Task<IActionResult> TeachingAssignment(string code, CancellationToken ct)
    {
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        var result = await SafeAsync(() => teachingApi.GetAssignmentWorkspaceAsync(token, code, ct));
        if (!result.Succeeded || result.Value is null) return ApiFailure(result.Error);
        return View(new TeachingAssignmentPageViewModel
        {
            Workspace = result.Value,
            Form = new CreateTeachingAssignmentViewModel { MaGiangVien = code }
        });
    }

    [HttpPost("giang-vien/{code}/phan-cong"), ValidateAntiForgeryToken]
    public async Task<IActionResult> TeachingAssignment(
        string code,
        CreateTeachingAssignmentViewModel model,
        CancellationToken ct)
    {
        model.MaGiangVien = code;
        var token = AccessToken;
        if (token is null) return LoginRedirect();
        if (ModelState.IsValid)
        {
            var created = await SafeAsync(() => teachingApi.CreateAssignmentAsync(token, model, ct));
            if (created.Succeeded)
            {
                TempData["Success"] = "Đã phân công giảng dạy, xếp lịch và tạo các buổi học trong học kỳ.";
                return RedirectToAction(nameof(TeachingAssignment), new { code });
            }
            ModelState.AddModelError(string.Empty, created.Error ?? "Không thể phân công giảng dạy.");
        }

        var workspace = await SafeAsync(() => teachingApi.GetAssignmentWorkspaceAsync(token, code, ct));
        if (!workspace.Succeeded || workspace.Value is null) return ApiFailure(workspace.Error);
        return View(new TeachingAssignmentPageViewModel { Workspace = workspace.Value, Form = model });
    }

    private string? AccessToken => HttpContext.Session.GetString("AccessToken");

    private IActionResult LoginRedirect() => RedirectToAction("Login", "Account");

    private IActionResult ApiFailure(string? error)
    {
        TempData["Error"] = error ?? "Không thể tải dữ liệu.";
        return RedirectToAction(nameof(Index));
    }

    private static async Task<ApiResult> SafeAsync(Func<Task<ApiResult>> action)
    {
        try { return await action(); }
        catch (HttpRequestException) { return new ApiResult(false, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new ApiResult(false, "Backend API phản hồi quá thời gian."); }
    }

    private static async Task<ApiResult<T>> SafeAsync<T>(Func<Task<ApiResult<T>>> action)
    {
        try { return await action(); }
        catch (HttpRequestException) { return new ApiResult<T>(false, default, "Không thể kết nối đến backend API."); }
        catch (TaskCanceledException) { return new ApiResult<T>(false, default, "Backend API phản hồi quá thời gian."); }
    }

    private static SinhVienFormViewModel ToStudentForm(SinhVienModel item) => new()
    {
        MaSinhVien = item.MaSinhVien,
        HoTen = item.HoTen,
        Email = item.Email,
        SoDienThoai = item.SoDienThoai,
        AnhDaiDien = item.AnhDaiDien,
        GioiTinh = item.GioiTinh,
        NgaySinh = item.NgaySinh,
        NoiSinh = item.NoiSinh,
        DanToc = item.DanToc,
        TonGiao = item.TonGiao,
        QuocTich = item.QuocTich,
        SoCCCD = item.SoCCCD,
        NgayCapCCCD = item.NgayCapCCCD,
        NoiCapCCCD = item.NoiCapCCCD,
        DiaChiLienHe = item.DiaChiLienHe,
        HoKhauThuongTru = item.HoKhauThuongTru,
        KhuVuc = item.KhuVuc,
        MaHoSo = item.MaHoSo,
        NgayVaoTruong = item.NgayVaoTruong,
        LopHoc = item.LopHoc,
        CoSo = item.CoSo,
        BacDaoTao = item.BacDaoTao,
        LoaiHinhDaoTao = item.LoaiHinhDaoTao,
        Khoa = item.Khoa,
        Nganh = item.Nganh,
        ChuyenNganh = item.ChuyenNganh,
        KhoaHoc = item.KhoaHoc,
        DoiTuong = item.DoiTuong,
        DienChinhSach = item.DienChinhSach,
        NgayVaoDoan = item.NgayVaoDoan,
        NgayVaoDang = item.NgayVaoDang
    };

    private static GiangVienFormViewModel ToLecturerForm(GiangVienModel item) => new()
    {
        MaGiangVien = item.MaGiangVien,
        HoTen = item.HoTen,
        Email = item.Email,
        SoDienThoai = item.SoDienThoai,
        AnhDaiDien = item.AnhDaiDien,
        GioiTinh = item.GioiTinh,
        NgaySinh = item.NgaySinh,
        NoiSinh = item.NoiSinh,
        DanToc = item.DanToc,
        TonGiao = item.TonGiao,
        QuocTich = item.QuocTich,
        SoCCCD = item.SoCCCD,
        NgayCapCCCD = item.NgayCapCCCD,
        NoiCapCCCD = item.NoiCapCCCD,
        DiaChiLienHe = item.DiaChiLienHe,
        HoKhauThuongTru = item.HoKhauThuongTru,
        NgayBatDauCongTac = item.NgayBatDauCongTac,
        CoSo = item.CoSo,
        Khoa = item.Khoa,
        ChuyenNganh = item.ChuyenNganh,
        TrinhDoHocVan = item.TrinhDoHocVan,
        ChucDanh = item.ChucDanh,
        NgayVaoDang = item.NgayVaoDang
    };
}
