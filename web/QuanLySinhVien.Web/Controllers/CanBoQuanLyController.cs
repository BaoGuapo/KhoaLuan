using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using QuanLySinhVien.Web.Models;

namespace QuanLySinhVien.Web.Controllers
{
    // Yêu cầu tài khoản phải có quyền CanBoQuanLy mới được vào
    [Authorize(Roles = "CanBoQuanLy")] 
    public class CanBoQuanLyController : Controller
    {
        private readonly HttpClient _httpClient;

        // Inject HttpClientFactory để khởi tạo kết nối API
        public CanBoQuanLyController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7081/");
        }

        public IActionResult Index() => RedirectToAction("CanBoQuanLy", "Home");

        // 1. Trang chủ Cán bộ quản lý
        public IActionResult CanBoQuanLy()
        {
            return RedirectToAction("CanBoQuanLy", "Home");
        }

        // 2. Trang Quản lý môn học (Lấy dữ liệu trực tiếp từ Backend API)
        public async Task<IActionResult> QuanLyMonHoc()
{
    var danhSachMonHoc = new List<MonHocViewModel>();
    try
    {
        var response = await _httpClient.GetAsync("api/MonHoc");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            danhSachMonHoc = JsonSerializer.Deserialize<List<MonHocViewModel>>(content, options) ?? new List<MonHocViewModel>();
        }
        else 
        {
            ViewBag.ErrorMessage = $"Lỗi từ API: Mã {response.StatusCode}. Chi tiết: {await response.Content.ReadAsStringAsync()}";
        }
    }
    catch (Exception ex)
    {
        ViewBag.ErrorMessage = $"Lỗi không thể kết nối tới Backend API: {ex.Message}";
    }

    return View(danhSachMonHoc);
}
        public IActionResult KeHoachDaoTao()
        {
   
            return View();
        }

        // 4. Trang Phân công lịch dạy (Bổ sung để sửa lỗi 404)
        public IActionResult PhanCongLichDay()
        {
            return View();
        }   

        // 5. Trang Mở cổng đăng ký cho Sinh viên
        public IActionResult MoCongDangKy()
        {
            return View();
        }
    }
}