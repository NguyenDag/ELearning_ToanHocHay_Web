using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Controllers
{
    public class CourseController : Controller
    {
        private readonly CourseApiService _courseApi;
        private const int DEFAULT_CURRICULUM_ID = 3; // Kết Nối Tri Thức

        public CourseController(CourseApiService courseApi)
        {
            _courseApi = courseApi;
        }

        // URL: /Course/Index
        public async Task<IActionResult> Index(int id = DEFAULT_CURRICULUM_ID)
        {
            var curriculum = await _courseApi.GetCurriculumDetailAsync(id);
            if (curriculum == null)
                return View(new CurriculumDto { Chapters = new List<ChapterDto>() });

            return View(curriculum);
        }

        // URL: /Course/Chapter/5
        public async Task<IActionResult> Chapter(int id)
        {
            var curriculum = await _courseApi.GetCurriculumDetailAsync(DEFAULT_CURRICULUM_ID);
            ViewBag.SelectedChapterId = id;
            return View("Index", curriculum);
        }

        // URL: /Course/Topic/10
        public async Task<IActionResult> Topic(int id)
        {
            var curriculum = await _courseApi.GetCurriculumDetailAsync(DEFAULT_CURRICULUM_ID);
            ViewBag.SelectedTopicId = id;
            return View("Index", curriculum);
        }

        // URL: /Course/Learning/id
        public async Task<IActionResult> Learning(int id)
        {
            var lesson = await _courseApi.GetLessonDetailAsync(id);
            if (lesson == null) return NotFound();

            var curriculum = await _courseApi.GetCurriculumDetailAsync(DEFAULT_CURRICULUM_ID);
            ViewBag.FullCurriculum = curriculum;
            ViewBag.CurrentTopicId = lesson.TopicId;
            return View("Lesson", lesson);
        }
    }
}