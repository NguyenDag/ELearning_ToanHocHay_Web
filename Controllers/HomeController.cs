// FILE: ToanHocHay.WebApp/Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Services;

namespace ToanHocHay.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly CourseApiService _courseApi;
        private const int KNTT_CURRICULUM_ID = 3; // Kết Nối Tri Thức

        public HomeController(CourseApiService courseApi)
        {
            _courseApi = courseApi;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy chapters từ curriculum Kết Nối Tri Thức để hiển thị sidebar
            var curriculum = await _courseApi.GetCurriculumDetailAsync(KNTT_CURRICULUM_ID);
            ViewData["Chapters"] = curriculum?.Chapters?.OrderBy(c => c.OrderIndex).Take(6).ToList();
            return View();
        }

        public IActionResult About() => View();
        public IActionResult Contact() => View();
    }
}