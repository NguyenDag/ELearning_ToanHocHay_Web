using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ToanHocHay.WebApp.Controllers
{
    [Authorize]
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

            // ← THÊM: Lấy danh sách lesson đã hoàn thành
            var studentIdStr = User.FindFirst("StudentId")?.Value;
            if (!string.IsNullOrEmpty(studentIdStr))
            {
                var completedLessonIds = await _courseApi.GetCompletedLessonIdsAsync(int.Parse(studentIdStr));
                ViewBag.CompletedLessonIds = completedLessonIds ?? new List<int>();
            }
            else
            {
                ViewBag.CompletedLessonIds = new List<int>();
            }

            // 3. Lấy các bài học cùng Topic để hiện ở Sidebar và điều hướng
            var relatedLessons = await _courseApi.GetLessonsByTopicAsync(lesson.TopicId);
            ViewBag.RelatedLessons = relatedLessons;

            return View("Lesson", lesson);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest req)
        {
            try
            {
                var studentIdStr = User.FindFirst("StudentId")?.Value;
                if (string.IsNullOrEmpty(studentIdStr))
                    return Unauthorized();
                req.StudentId = int.Parse(studentIdStr);

                Console.WriteLine($"=== UpdateProgress: studentId={req.StudentId}, lessonId={req.LessonId}, watchTime={req.WatchTime} ==="); // ← THÊM

                var client = HttpContext.RequestServices
                    .GetRequiredService<IHttpClientFactory>().CreateClient();
                var token = HttpContext.Session.GetString("Token")
                         ?? User.FindFirst("Token")?.Value;
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var beResponse = await client.PostAsJsonAsync(
                    $"{ToanHocHay.WebApp.Common.Constants.ApiConstant.apiBaseUrl}/api/LessonProgress/update-progress",
                    req);

                Console.WriteLine($"=== UpdateProgress BE response: {beResponse.StatusCode} ==="); // ← THÊM

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== UpdateProgress ERROR: {ex.Message} ==="); // ← THÊM
                return Ok();
            }
        }
    }
    public class UpdateProgressRequest
    {
        public int StudentId { get; set; }
        public int LessonId { get; set; }
        public int WatchTime { get; set; }
    }
}