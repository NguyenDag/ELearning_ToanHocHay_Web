using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Controllers
{
    public class CourseController : Controller
    {
        private readonly CourseApiService _courseApi;

        public CourseController(CourseApiService courseApi)
        {
            _courseApi = courseApi;
        }

        // Trang hiển thị lộ trình học tập chi tiết (Chapters -> Topics -> Lessons)
        // URL: /Course/Index
        public async Task<IActionResult> Index()
        {
            // Lấy chi tiết lộ trình ID = 1 (Ví dụ: Toán lớp 6)
            // Lưu ý: Dữ liệu này cần được API trả về dạng Nested JSON (kèm Chapters)
            var curriculum = await _courseApi.GetCurriculumDetailAsync(1);

            if (curriculum == null)
            {
                // Nếu không tìm thấy, gửi một object rỗng để View không bị lỗi NullReference
                return View(new CurriculumDto { Chapters = new List<ChapterDto>() });
            }

            // Gửi duy nhất 1 CurriculumDto khớp với khai báo @model trong Canvas
            return View(curriculum);
        }

        // Trang học bài chi tiết (Learning Player)
        // URL: /Course/Learning/id
        public async Task<IActionResult> Learning(int id)
        {
            var lesson = await _courseApi.GetLessonDetailAsync(id);
            if (lesson == null) return NotFound();

            // Lấy danh sách bài học liên quan trong cùng chủ đề để hiện ở Sidebar
            ViewBag.RelatedLessons = await _courseApi.GetLessonsByTopicAsync(lesson.TopicId);

            return View(lesson);
        }
    }
}