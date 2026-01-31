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

        public StudentController(AuthApiService authApiService)
        {
            _authApiService = authApiService;
        }

        // URL: /Student/Profile
        public async Task<IActionResult> Profile()
        {
            // Lấy UserId từ Token đã lưu khi đăng nhập
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            var userProfile = await _authApiService.GetProfileAsync(int.Parse(userIdStr));
            return View(userProfile);
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