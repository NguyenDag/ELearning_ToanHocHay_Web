// FILE: ToanHocHay.WebApp/Controllers/StudentController.cs
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly AuthApiService _authApiService;
        private readonly CourseApiService _courseApiService;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenStore _tokenStore;

        public StudentController(
            AuthApiService authApiService,
            CourseApiService courseApiService,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ITokenStore tokenStore)
        {
            _authApiService = authApiService;
            _courseApiService = courseApiService;
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _httpContextAccessor = httpContextAccessor;
            _tokenStore = tokenStore;
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!int.TryParse(User.FindFirst("StudentId")?.Value, out var studentId))
                return RedirectToAction("Login", "Account");

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

            if (result.Success)
            {
                // Backend thu hồi refresh token + bump SecurityStamp → phải đăng nhập lại.
                await _authApiService.LogoutAsync(_tokenStore.RefreshToken);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _tokenStore.Clear();
                HttpContext.Session.Clear();
                Response.Cookies.Delete("ToanHocHay_Auth_Cookie");
                TempData["SuccessMsg"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";

                return Json(new { success = true, message = result.Message, redirect = Url.Action("Login", "Account") });
            }

            return Json(new { success = false, message = result.Message, errors = result.Errors });
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

                // Route đổi: /api/StudentParent/connect → /api/parents/link (LinkParentDto { Code, Relationship }).
                // Luồng liên kết đầy đủ (mời qua email, huỷ liên kết...) làm ở Đợt 7.
                var response = await _httpClient.PostAsJsonAsync(
    $"{ApiConstant.apiBaseUrl}/api/{ApiRoutes.Parents.Link}",
    new { Code = model.ConnectionCode, Relationship = model.Relationship },
    ToanHocHay.WebApp.Services.Http.ApiJson.Options);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"=== CONNECT RESPONSE: Status={response.StatusCode}, Body={json} ===");

                if (response.IsSuccessStatusCode)
                    return Ok(new { success = true, message = "Kết nối thành công!" });

                // Check empty TRƯỚC khi parse
                if (string.IsNullOrEmpty(json))
                    return BadRequest(new { success = false, message = "Lỗi không xác định" });

                var result = JsonSerializer.Deserialize<JsonElement>(json);
                var msg = result.TryGetProperty("Message", out var m) ? m.GetString()
                        : result.TryGetProperty("message", out var m2) ? m2.GetString()
                        : "Mã không hợp lệ";
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

                // TODO(Đợt 7): backend chưa có GET /api/students/{id}/parents — đề nghị bổ sung,
                // hoặc lấy từ GET /api/parents/{parentId}/children ở phía phụ huynh.
                var response = await _httpClient.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/students/{studentId}/parents");
                if (!response.IsSuccessStatusCode) return new();

                var wrapper = await response.Content
                    .ReadFromJsonAsync<ApiResponse<List<ConnectedParentDto>>>(
                        ToanHocHay.WebApp.Services.Http.ApiJson.Options);
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