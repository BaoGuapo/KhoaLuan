using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLySinhVien.Web.Controllers
{
    // Yêu cầu tài khoản phải có quyền CanBoQuanLy (hoặc GiaoVu) mới được vào
    [Authorize(Roles = "CanBoQuanLy")] 
    public class CanBoQuanLyController : Controller
    {
        // 1. Trang chủ Cán bộ quản lý
        public IActionResult CanBoQuanLy()
        {
            return View();
        }

        // 2. Trang Quản lý môn học
        public IActionResult QuanLyMonHoc()
        {
            return View();
        }

        // 3. Trang Tạo lớp học phần
        public IActionResult TaoLopHocPhan()
        {
            return View();
        }

        // 4. Trang Phân công giảng viên & Thời khóa biểu
        public IActionResult PhanCongGiangVien()
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