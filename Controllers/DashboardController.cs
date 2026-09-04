// FILE: ToanHocHay.WebApp/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;

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

        private int? StudentId =>
            int.TryParse(User.FindFirst("StudentId")?.Value, out var id) ? id : null;

        public async Task<IActionResult> Index()
        {
            if (StudentId is not { } studentId)
                return RedirectToAction("Login", "Account");

            var res = await _apiService.GetStudentDashboardAsync(studentId);

            if (res.IsUnauthorized)
                return RedirectToAction("Login", "Account");
            if (!res.IsSuccess || res.Data == null)
            {
                this.ShowToastError(res);
                return View("~/Views/Student/Dashboard.cshtml", new CoreDashboardDto());
            }

            var data = res.Data;

            // Bổ sung SubscriptionInfo nếu backend chưa kèm.
            if (data.SubscriptionInfo == null || (!data.SubscriptionInfo.IsActive && data.SubscriptionInfo.PackageTier == PackageTier.Free))
            {
                var sub = await _subscriptionService.GetCurrentSubscriptionAsync(studentId);
                if (sub != null)
                {
                    data.SubscriptionInfo = sub;
                    data.PackageTier = sub.PackageTier;
                }
                else
                {
                    data.SubscriptionInfo ??= new SubscriptionInfoDto();
                }
            }

            if (data.RecentLessons != null)
            {
                data.RecentLessons = data.RecentLessons
                    .GroupBy(l => l.LessonId)
                    .Select(g => g.OrderByDescending(x => x.CompletedAt ?? DateTime.MinValue).First())
                    .Take(5)
                    .ToList();
            }

            // Tier gating cho phần UI nâng cao — backend đã null-hoá Link tương ứng theo gói.
            ViewBag.CanChart = data.Links?.Charts != null || data.PackageTier >= PackageTier.Standard;
            ViewBag.CanAI = data.Links?.AIInsights != null || data.PackageTier >= PackageTier.Premium;

            return View("~/Views/Student/Dashboard.cshtml", data);
        }

        // GET /Dashboard/ChartData — AJAX cho biểu đồ (Standard+)
        [HttpGet]
        public async Task<IActionResult> ChartData()
        {
            if (StudentId is not { } studentId) return Unauthorized();

            var res = await _apiService.GetChapterScoreComparisonAsync(studentId);

            if (res.IsForbidden)
                return Json(new { success = false, upgradeRequired = true, message = "Biểu đồ điểm theo chương cần gói Tiêu chuẩn trở lên." });
            if (!res.IsSuccess)
                return Json(new { success = false, message = res.DisplayMessage });

            var data = res.Data ?? new List<ChapterScoreDto>();
            return data.Count == 0
                ? Json(new { success = false, reason = "data_empty" })
                : Json(new { success = true, data });
        }

        [HttpGet]
        public Task<IActionResult> AIAssessment() => AiInsight(assessment: true);

        [HttpGet]
        public Task<IActionResult> AIRoadmap() => AiInsight(assessment: false);

        private async Task<IActionResult> AiInsight(bool assessment)
        {
            if (StudentId is not { } studentId) return Unauthorized();

            var res = assessment
                ? await _apiService.GetAIAssessmentAsync(studentId)
                : await _apiService.GetAIRoadmapAsync(studentId);

            if (res.IsForbidden)
                return Json(new { success = false, upgradeRequired = true, message = "Tính năng phân tích AI cần gói Premium." });
            if (res.IsNotFound)
                return Json(new { success = false, message = "Chưa đủ dữ liệu học tập để AI phân tích. Hãy làm thêm bài tập nhé!" });
            if (!res.IsSuccess || res.Data == null)
                return Json(new { success = false, message = res.DisplayMessage });

            return Json(new { success = true, data = res.Data });
        }
    }
}
