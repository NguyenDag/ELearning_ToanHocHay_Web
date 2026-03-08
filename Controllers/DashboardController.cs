// FILE: ToanHocHay.WebApp/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;

namespace ToanHocHay.WebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardApiService _apiService;
        private readonly SubscriptionApiService _subscriptionService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardApiService apiService,
            SubscriptionApiService subscriptionService,
            ILogger<DashboardController> logger)
        {
            _apiService = apiService;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (string.IsNullOrEmpty(studentIdClaim))
                {
                    _logger.LogWarning("Không tìm thấy StudentId trong Claims.");
                    return RedirectToAction("Login", "Account");
                }

                int studentId = int.Parse(studentIdClaim);

                // 1. Lấy dashboard data
                var data = await _apiService.GetStudentDashboardAsync(studentId);
                if (data == null)
                {
                    _logger.LogError("API trả về NULL cho StudentId: {Id}", studentId);
                    data = new CoreDashboardDto();
                }

                // 2. Nếu backend chưa trả SubscriptionInfo (PackageType = 0, IsActive = false)
                //    → gọi riêng subscription endpoint để lấy đúng gói
                bool subInfoMissing = data.SubscriptionInfo == null
                                   || (!data.SubscriptionInfo.IsActive && data.SubscriptionInfo.PackageType == 0);

                if (subInfoMissing)
                {
                    var sub = await _subscriptionService.GetCurrentSubscriptionAsync(studentId);
                    if (sub != null)
                    {
                        data.SubscriptionInfo = sub;
                        // Đồng bộ PackageType ở root DTO để tương thích backward
                        data.PackageType = sub.PackageType;
                    }
                    else
                    {
                        // Không có subscription → Free
                        data.SubscriptionInfo ??= new SubscriptionInfoDto
                        {
                            PackageType = 0,
                            PackageName = "Free",
                            IsActive = false,
                            AiHintLimitDaily = 0,
                        };
                    }
                }

                // 3. Xóa bài tập lặp (GroupBy LessonId)
                if (data.RecentLessons != null)
                {
                    data.RecentLessons = data.RecentLessons
                        .GroupBy(l => l.LessonId)
                        .Select(g => g.OrderByDescending(x => x.CompletedAt).First())
                        .Take(5)
                        .ToList();
                }

                return View("~/Views/Student/Dashboard.cshtml", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi Dashboard");
                return View("Error");
            }
        }

        // GET /Dashboard/ChartData — AJAX cho biểu đồ Standard+
        [HttpGet]
        public async Task<IActionResult> ChartData()
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdClaim))
                return Unauthorized();

            int studentId = int.Parse(studentIdClaim);
            var data = await _apiService.GetChapterScoreComparisonAsync(studentId);
            if (data == null)
                return Json(new { success = false });
            return Json(new { success = true, data });
        }

        // GET /Dashboard/AIInsightData — AJAX cho AI Insight Premium
        [HttpGet]
        public async Task<IActionResult> AIInsightData()
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdClaim))
                return Unauthorized();

            int studentId = int.Parse(studentIdClaim);
            var data = await _apiService.GetAIInsightAsync(studentId);
            
            if (data == null)
                return Json(new { success = false, message = "Không thể lấy dữ liệu phân tích AI." });

            return Json(new { success = true, data });
        }
    }
}