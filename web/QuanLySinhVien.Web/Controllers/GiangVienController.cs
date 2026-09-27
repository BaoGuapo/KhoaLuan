using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLySinhVien.Web.Controllers
{
    [Authorize(Roles = "GiangVien")] 
    public class GiangVienController : Controller
    {
        public IActionResult LichGiangDay()
        {
            return View();
        }
        public IActionResult QuanLyLop(string maLop)
        {
            ViewBag.MaLop = maLop;
            return View();
        }
    }
}