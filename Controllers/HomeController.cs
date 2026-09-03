// FILE: ToanHocHay.WebApp/Controllers/HomeController.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Common.Http;
using ToanHocHay.WebApp.Models;
using ToanHocHay.WebApp.Services;

namespace ToanHocHay.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly CourseApiService _courseApi;
        private readonly ILogger<HomeController> _logger;
        private const int KNTT_CURRICULUM_ID = 3; // Kết Nối Tri Thức

        public HomeController(CourseApiService courseApi, ILogger<HomeController> logger)
        {
            _courseApi = courseApi;
            _logger = logger;
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