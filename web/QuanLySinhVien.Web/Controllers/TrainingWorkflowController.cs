using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;

namespace QuanLySinhVien.Web.Controllers;

[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
[Route("dao-tao")]
public sealed class TrainingWorkflowController(TrainingWorkflowApiClient api) : Controller
{
    private string? Token => HttpContext.Session.GetString("AccessToken");
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? id, CancellationToken ct)
    {
        if (Token is not { } token) return RedirectToAction("Login", "Account");
        return View(await Workspace(token, id, ct));
    }
    [HttpPost("nganh-hoc"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Major(string maNganh, string tenNganh, CancellationToken ct) =>
        await Send("nganh-hoc", new { maNganh, tenNganh }, "Đã tạo ngành.", null, ct);
    [HttpPost("khoa-tuyen-sinh"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Intake(string maKhoa, int namBatDau, int namKetThuc, CancellationToken ct) =>
        await Send("khoa-tuyen-sinh", new { maKhoa, namBatDau, namKetThuc }, "Đã tạo khóa tuyển sinh.", null, ct);
    [HttpPost("hoc-ky"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Semester(string maHocKy, string tenHocKy, DateTime ngayBatDau, DateTime ngayKetThuc, CancellationToken ct) =>
        await Send("hoc-ky", new { maHocKy, tenHocKy, ngayBatDau, ngayKetThuc }, "Đã tạo học kỳ thực tế.", null, ct);
    [HttpPost("luu-chuong-trinh"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Curriculum(CurriculumModel draft, CancellationToken ct)
    {
        if (Token is not { } token) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) { TempData["Error"] = "Dữ liệu form không hợp lệ."; }
        else
        {
            var result = await api.PostAsync(token, "chuong-trinh", draft, ct);
            if (result.Succeeded) { TempData["Success"] = "Đã lưu chương trình đào tạo."; return RedirectToAction(nameof(Index)); }
            TempData["Error"] = result.Error;
        }
        var model = await Workspace(token, null, ct); model.Draft = draft;
        return View("Index", model);
    }
    [HttpPost("ap-dung-hoc-ky"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(Guid keHoachDaoTaoId, string maHocKy, int thuTuHocKy, CancellationToken ct) =>
        await Send("ap-dung-hoc-ky", new { keHoachDaoTaoId, maHocKy, thuTuHocKy }, "Đã áp dụng học kỳ. Chọn đợt bên dưới để mở lớp.", null, ct);
    [HttpPost("mo-lop"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Classes(Guid id, string maMonHoc, string tienToMaLop, int soLop, int siSoToiDa, CancellationToken ct) =>
        await Send($"ap-dung-hoc-ky/{id}/mo-lop", new { maMonHoc, tienToMaLop, soLop, siSoToiDa }, $"Đã mở {soLop} lớp chờ phân công.", id, ct);
    [HttpPost("phan-cong"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid id, string maLopHocPhan, string maGiangVien, string maPhong, int thuTrongTuan, int tietBatDau, int tietKetThuc, CancellationToken ct) =>
        await Send($"lop-hoc-phan/{Uri.EscapeDataString(maLopHocPhan)}/phan-cong", new { maGiangVien, maPhong, thuTrongTuan, tietBatDau, tietKetThuc }, "Đã phân công giảng viên và tạo lịch học.", id, ct);
    [HttpPost("mo-dang-ky"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, string maLopHocPhan, CancellationToken ct) =>
        await Send($"lop-hoc-phan/{Uri.EscapeDataString(maLopHocPhan)}/mo-dang-ky", new { }, "Đã mở lớp cho sinh viên đăng ký.", id, ct);
    [HttpGet("mau-excel")]
    public async Task<IActionResult> Template(CancellationToken ct)
    {
        if (Token is not { } token) return RedirectToAction("Login", "Account");
        var result = await api.TemplateAsync(token, ct);
        if (!result.Succeeded || result.Value is null) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Index)); }
        return File(result.Value, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ChuongTrinhDaoTao-mau.xlsx");
    }
    [HttpPost("xem-truoc-excel"), ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Preview(IFormFile? file, CurriculumModel draft, CancellationToken ct)
    {
        if (Token is not { } token) return RedirectToAction("Login", "Account");
        var model = await Workspace(token, null, ct); model.Draft = draft;
        if (file is null || file.Length is < 1 or > 2 * 1024 * 1024) model.ImportErrors.Add("Chọn file Excel tối đa 2 MB.");
        else
        {
            var result = await api.PreviewAsync(token, file, ct);
            if (!result.Succeeded || result.Value is null) model.ImportErrors.Add(result.Error ?? "Không đọc được file.");
            else
            {
                model.Draft.MonHocs = result.Value.MonHocs;
                model.ImportErrors.AddRange(result.Value.Errors);
                if (model.ImportErrors.Count == 0) TempData["Success"] = "Đã đọc Excel. Kiểm tra môn bên dưới rồi bấm Lưu chương trình; chưa ghi database.";
            }
        }
        return View("Index", model);
    }
    private async Task<IActionResult> Send(string endpoint, object body, string message, Guid? id, CancellationToken ct)
    {
        if (Token is not { } token) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) { TempData["Error"] = "Dữ liệu form không hợp lệ."; return RedirectToAction(nameof(Index), new { id }); }
        var result = await api.PostAsync(token, endpoint, body, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? message : result.Error;
        return RedirectToAction(nameof(Index), new { id });
    }
    private async Task<TrainingWorkspaceModel> Workspace(string token, Guid? id, CancellationToken ct)
    {
        var model = new TrainingWorkspaceModel();
        var catalog = await api.GetAsync<TrainingCatalogModel>(token, "danh-muc", ct);
        if (!catalog.Succeeded) { TempData["Error"] = catalog.Error; return model; }
        model.Catalog = catalog.Value!;
        var curricula = await api.GetAsync<List<CurriculumModel>>(token, "chuong-trinh", ct);
        var applications = await api.GetAsync<List<SemesterApplicationModel>>(token, "ap-dung-hoc-ky", ct);
        if (!curricula.Succeeded || !applications.Succeeded) { TempData["Error"] = curricula.Error ?? applications.Error; return model; }
        model.Curricula = curricula.Value ?? []; model.Applications = applications.Value ?? [];
        model.Selected = model.Applications.FirstOrDefault(x => x.Id == id) ?? model.Applications.LastOrDefault(); model.Loaded = true;
        return model;
    }
}
