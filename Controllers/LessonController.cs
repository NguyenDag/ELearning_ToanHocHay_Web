using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Models.DTOs; // Sử dụng DTO của chính WebApp

namespace ToanHocHay.WebApp.Controllers
{
    public class LessonController : Controller
    {
        private readonly LessonApiService _lessonApiService;

        public LessonController(LessonApiService lessonApiService)
        {
            _lessonApiService = lessonApiService;
        }

        // Action hiển thị chi tiết bài học
        public async Task<IActionResult> Detail(int id)
        {
            // 1. Gọi API lấy chi tiết bài học
            var lesson = await _lessonApiService.GetByIdAsync(id);
            if (lesson == null) return NotFound();

            // 2. Lấy thêm các bài học cùng Topic để hiện ở Sidebar
            // Lưu ý: relatedLessons lúc này sẽ là danh sách LessonDto của WebApp
            var relatedLessons = await _lessonApiService.GetByTopicAsync(lesson.TopicId);

            // Đổ vào ViewBag để file Lesson.cshtml có thể duyệt loop ở Sidebar
            ViewBag.RelatedLessons = relatedLessons;

            return View(lesson); // Trả về file Lesson.cshtml với Model là LessonDto
        }
    }
}