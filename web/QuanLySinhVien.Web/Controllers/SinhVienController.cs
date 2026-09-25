using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLySinhVien.Web.Controllers
{
    [Authorize(Roles = "SinhVien")] 
    public class SinhVienController : Controller
    {
        public IActionResult DangKyHocPhan()
        {
            return View(); 
        }
        public IActionResult XemLichHoc()
        {
            return View();
        }
    }
}