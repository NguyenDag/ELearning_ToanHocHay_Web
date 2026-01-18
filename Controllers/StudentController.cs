using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ToanHocHay.WebApp.Controllers
{
    // [Authorize] // Bỏ comment dòng này nếu muốn bắt buộc đăng nhập mới vào được
    public class StudentController : Controller
    {
        // URL: /Student/Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        // URL: /Student/Profile
        public IActionResult Profile()
        {
            return View();
        }
    }
}