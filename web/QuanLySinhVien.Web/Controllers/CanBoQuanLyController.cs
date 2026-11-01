using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLySinhVien.Web.Controllers
{
    // Yêu cầu tài khoản phải có quyền CanBoQuanLy (hoặc GiaoVu) mới được vào
    [Authorize(Roles = "CanBoQuanLy")] 
    public class CanBoQuanLyController : Controller
    {
        public IActionResult Index() => RedirectToAction("CanBoQuanLy", "Home");

        // 1. Trang chủ Cán bộ quản lý
        public IActionResult CanBoQuanLy()
        {
            return RedirectToAction("CanBoQuanLy", "Home");
        }

        // 2. Trang Quản lý môn học
        public IActionResult QuanLyMonHoc()
        {
            return View();
        }

        // 3. Trang Tạo lớp học phần
        public IActionResult TaoLopHocPhan()
        {
            return RedirectToAction("Index", "TrainingWorkflow");
        }

        // 4. Trang Phân công giảng viên & Thời khóa biểu
        public IActionResult PhanCongGiangVien()
        {
            return RedirectToAction("Index", "TrainingWorkflow");
        }

        // 5. Trang Mở cổng đăng ký cho Sinh viên
        public IActionResult MoCongDangKy()
        {
            return RedirectToAction("Index", "TrainingWorkflow");
        }
    }
}
