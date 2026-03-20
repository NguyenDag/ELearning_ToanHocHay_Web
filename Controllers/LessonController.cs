using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;

namespace ToanHocHay.WebApp.Controllers
{
    public class LessonController : Controller
    {
        private readonly CourseApiService _courseApiService;

        public LessonController(CourseApiService courseApiService)
        {
            _courseApiService = courseApiService;
        }

        [HttpGet("Lesson/Detail/{id}")]
        public IActionResult Detail(int id)
        {
            return RedirectToAction("Learning", "Course", new { id = id });
        }
        public async Task<IActionResult> Editor()
        {
            /*var client = _httpClientFactory.CreateClient("Api");

            var chapters = await client.GetFromJsonAsync<List<ChapterSelectDto>>(
                "api/chapters/select");*/

            return View();
        }
    }
}
