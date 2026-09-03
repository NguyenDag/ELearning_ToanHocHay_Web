// FILE: ToanHocHay.WebApp/Controllers/HomeController.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Common.Http;
using ToanHocHay.WebApp.Models;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;

namespace ToanHocHay.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ContentApiService _content;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ContentApiService content, ILogger<HomeController> logger)
        {
            _content = content;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Sidebar: 6 chương đầu của khoá học published đầu tiên.
            var courses = await _content.GetCoursesAsync(publishedOnly: true);
            var courseId = courses.Data?
                .Where(c => c.PublishedVersionId != null)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => (int?)c.CourseId)
                .FirstOrDefault() ?? courses.Data?.FirstOrDefault()?.CourseId;

            if (courseId is > 0)
            {
                var content = await _content.GetCourseContentAsync(courseId.Value);
                if (content.IsSuccess && content.Data != null)
                {
                    ViewData["Chapters"] = content.Data.Tree
                        .Where(n => !n.IsHidden)
                        .OrderBy(n => n.OrderIndex)
                        .Take(6)
                        .Select(n => new ChapterDto
                        {
                            ChapterId = n.NodeId,
                            ChapterName = n.Title,
                            OrderIndex = n.OrderIndex,
                            Topics = n.Children.Where(c => !c.IsHidden)
                                .Select(c => new TopicDto { TopicId = c.NodeId, TopicName = c.Title })
                                .ToList()
                        })
                        .ToList();
                }
            }
            return View();
        }

        public IActionResult About() => View();
        public IActionResult Contact() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Home/Error/{code:int?}")]
        public IActionResult Error(int? code)
        {
            var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var correlationId = HttpContext.CorrelationId();

            if (feature?.Error != null)
                _logger.LogError(feature.Error,
                    "Lỗi chưa xử lý tại {Path} (correlationId={CorrelationId})",
                    feature.Path, correlationId);

            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                CorrelationId = correlationId,
                StatusCode = code,
                Message = code switch
                {
                    404 => "Không tìm thấy trang bạn yêu cầu.",
                    401 => "Bạn cần đăng nhập để tiếp tục.",
                    403 => "Bạn không có quyền truy cập nội dung này.",
                    >= 500 => "Máy chủ đang gặp sự cố. Vui lòng thử lại sau ít phút.",
                    _ => "Đã có lỗi xảy ra khi xử lý yêu cầu của bạn."
                }
            };
            return View(model);
        }
    }
}