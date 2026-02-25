using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Models.DTOs;
using System.Security.Claims;

namespace ToanHocHay.WebApp.Controllers
{
    [Authorize] // Bắt buộc đăng nhập để vào hồ sơ
    public class StudentController : Controller
    {
        private readonly AuthApiService _authApiService;

        private readonly CourseApiService _courseApiService;

        public StudentController(AuthApiService authApiService, CourseApiService courseApiService)
        {
            _authApiService = authApiService;
            _courseApiService = courseApiService;
        }
        public async Task<IActionResult> Dashboard()
        {
            // Lấy StudentId từ Claims (Đã được AccountController lưu lúc Login)
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            int studentId = string.IsNullOrEmpty(studentIdClaim) ? 5 : int.Parse(studentIdClaim);

            // Gọi Service với kiểu dữ liệu CoreDashboardDto mới
            var stats = await _courseApiService.GetStudentDashboardStatsAsync(studentId);

            // Truyền sang View (Đảm bảo View cũng sử dụng @model CoreDashboardDto)
            return View(stats ?? new CoreDashboardDto());
        }

        // URL: /Student/Profile
        public async Task<IActionResult> Profile()
        {
            // 1. Lấy ID người dùng từ Claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim.Value);

            // 2. Gọi API để lấy thông tin (Quan trọng: phải có dữ liệu trả về)
            var userProfile = await _authApiService.GetProfileAsync(userId);

            // 3. Kiểm tra nếu Service trả về null thì phải khởi tạo một Object rỗng 
            // để tránh lỗi "Object reference not set..." tại View
            if (userProfile == null)
            {
                userProfile = new UserDto { FullName = "Học sinh", Email = "" };
            }

            return View(userProfile); // Truyền userProfile sang View
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var result = await _authApiService.UpdateProfileAsync(int.Parse(userIdStr), model);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var result = await _authApiService.ChangePasswordAsync(int.Parse(userIdStr), model);
            return Json(result);
        }
    }
}