using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Models.DTOs;

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
        public async Task<IActionResult> Detail(int id)
        {
            // 1. Lấy thông tin bài học hiện tại
            var lesson = await _courseApiService.GetLessonDetailAsync(id);
            if (lesson == null) return NotFound();

            // 2. Lấy các bài học cùng Topic để hiện ở Sidebar
            var relatedLessons = await _courseApiService.GetLessonsByTopicAsync(lesson.TopicId);

            // Đổ vào ViewBag theo đúng yêu cầu của giao diện
            ViewBag.RelatedLessons = relatedLessons;

            return View(lesson);
        }
    }
}