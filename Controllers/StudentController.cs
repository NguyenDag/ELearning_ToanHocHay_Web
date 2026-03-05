// FILE: ToanHocHay.WebApp/Controllers/StudentController.cs
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;

namespace ToanHocHay.WebApp.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly AuthApiService _authApiService;
        private readonly CourseApiService _courseApiService;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StudentController(
            AuthApiService authApiService,
            CourseApiService courseApiService,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _authApiService = authApiService;
            _courseApiService = courseApiService;
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Dashboard()
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            int studentId = string.IsNullOrEmpty(studentIdClaim) ? 5 : int.Parse(studentIdClaim);
            var stats = await _courseApiService.GetStudentDashboardStatsAsync(studentId);
            return View(stats ?? new CoreDashboardDto());
        }

        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim.Value);
            var userProfile = await _authApiService.GetProfileAsync(userId);
            if (userProfile == null)
                userProfile = new UserDto { FullName = "Học sinh", Email = "" };

            // Lấy danh sách phụ huynh đã kết nối
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (!string.IsNullOrEmpty(studentIdClaim))
            {
                var parents = await GetConnectedParentsAsync(int.Parse(studentIdClaim));
                ViewData["ConnectedParents"] = parents;
            }

            return View(userProfile);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var result = await _authApiService.UpdateProfileAsync(int.Parse(userIdStr), model);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var result = await _authApiService.ChangePasswordAsync(int.Parse(userIdStr), model);
            return Json(result);
        }

        // POST /Student/ConnectParent — học sinh nhập mã phụ huynh
        [HttpPost]
        public async Task<IActionResult> ConnectParent([FromBody] ConnectParentDto model)
        {
            var studentIdStr = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdStr))
                return Unauthorized(new { success = false, message = "Không xác định được học sinh" });

            try
            {
                var token = _httpContextAccessor.HttpContext?.Session.GetString("Token")
                         ?? _httpContextAccessor.HttpContext?.User.FindFirst("Token")?.Value;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token?.Trim() ?? "");

                var response = await _httpClient.PostAsJsonAsync(
                    $"{ApiConstant.apiBaseUrl}/api/student-parent/connect",
                    new { connectionCode = model.ConnectionCode, relationship = model.Relationship });

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);

                if (response.IsSuccessStatusCode)
                    return Ok(new { success = true, message = "Kết nối thành công!" });

                var msg = result.TryGetProperty("message", out var m) ? m.GetString() : "Mã không hợp lệ";
                return BadRequest(new { success = false, message = msg });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET danh sách phụ huynh đã kết nối
        private async Task<List<ConnectedParentDto>> GetConnectedParentsAsync(int studentId)
        {
            try
            {
                var token = _httpContextAccessor.HttpContext?.Session.GetString("Token")
                         ?? _httpContextAccessor.HttpContext?.User.FindFirst("Token")?.Value;
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token?.Trim() ?? "");

                var response = await _httpClient.GetAsync($"student/{studentId}/parents");
                if (!response.IsSuccessStatusCode) return new();

                var wrapper = await response.Content
                    .ReadFromJsonAsync<ApiResponse<List<ConnectedParentDto>>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return wrapper?.Data ?? new();
            }
            catch { return new(); }
        }
    }

    public class ConnectParentDto
    {
        public string ConnectionCode { get; set; } = "";
        public int Relationship { get; set; } = 2; // 0=Father,1=Mother,2=Guardian,3=Other
    }

    public class ConnectedParentDto
    {
        public int ParentId { get; set; }
        public string FullName { get; set; } = "";
        public string Relationship { get; set; } = "";
    }
}