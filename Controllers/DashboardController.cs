using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using ToanHocHay.WebApp.Models.DTOs; // Đã sửa: Sử dụng namespace phẳng để khớp với DTO của bạn
using ToanHocHay.WebApp.Services;

namespace ToanHocHay.WebApp.Controllers
{
    /// <summary>
    /// Controller xử lý dữ liệu bảng điều khiển (Dashboard) cho học sinh.
    /// Logic tại đây giúp làm sạch dữ liệu thô từ API trước khi hiển thị.
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardApiService _apiService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardApiService apiService, ILogger<DashboardController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. Lấy StudentId từ Claims (Được AccountController lưu khi login thành công)
                var studentIdClaim = User.FindFirst("StudentId")?.Value;

                if (string.IsNullOrEmpty(studentIdClaim))
                {
                    _logger.LogWarning("User đăng nhập nhưng không tìm thấy StudentId trong Claims.");
                    return RedirectToAction("Login", "Account");
                }

                int studentId = int.Parse(studentIdClaim);

                // 2. Gọi API lấy dữ liệu thật từ dự án Control
                var data = await _apiService.GetStudentDashboardAsync(studentId);

                if (data == null)
                {
                    _logger.LogError("API trả về NULL cho StudentId: {Id}", studentId);
                    // Trả về một Object trống để View không bị crash
                    data = new CoreDashboardDto();
                }

                // 3. LOGIC LÀM SẠCH (GroupBy để xóa bài lặp LessonId 3)
                if (data.RecentLessons != null)
                {
                    data.RecentLessons = data.RecentLessons
                        .GroupBy(l => l.LessonId)
                        .Select(g => g.OrderByDescending(x => x.CompletedAt).First())
                        .Take(5)
                        .ToList();
                }

                // FIX LỖI VIEW NOT FOUND: 
                // Sử dụng đường dẫn tuyệt đối (bắt đầu bằng ~/) để tìm đúng file trong thư mục Student.
                return View("~/Views/Student/Dashboard.cshtml", data);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi xử lý Dashboard");
                return View("Error");
            }
        }
    }
}