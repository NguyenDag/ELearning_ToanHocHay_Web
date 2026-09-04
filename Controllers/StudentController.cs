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
using ToanHocHay.WebApp.Models.DTOs.Parent;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly AuthApiService _authApiService;
        private readonly CourseApiService _courseApiService;
        private readonly ParentApiService _parents;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenStore _tokenStore;

        public StudentController(
            AuthApiService authApiService,
            CourseApiService courseApiService,
            ParentApiService parents,
            IHttpContextAccessor httpContextAccessor,
            ITokenStore tokenStore)
        {
            _authApiService = authApiService;
            _courseApiService = courseApiService;
            _parents = parents;
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

        // POST /Student/ConnectParent — học sinh nhập mã liên kết của phụ huynh (hoặc token lời mời)
        [HttpPost]
        public async Task<IActionResult> ConnectParent([FromBody] ConnectParentDto model)
        {
            if (User.FindFirst("StudentId") == null)
                return Unauthorized(new { success = false, message = "Không xác định được học sinh" });

            var r = await _parents.LinkByCodeAsync(new LinkParentInputDto
            {
                Code = model.ConnectionCode?.Trim() ?? "",
                Relationship = (ParentRelationship)Math.Clamp(model.Relationship, 0, 3)
            });

            if (r.IsSuccess)
                return Ok(new { success = true, message = "Kết nối với phụ huynh thành công!" });

            // 404 mã sai · 409 đã liên kết · 410/400 hết hạn
            return StatusCode(r.StatusCode == 0 ? 500 : r.StatusCode,
                new { success = false, message = r.DisplayMessage });
        }

        // GET danh sách phụ huynh đã kết nối — backend CHƯA có GET /api/students/{id}/parents.
        // TODO(backend): bổ sung endpoint này; tạm thời trả rỗng (trang Hồ sơ ẩn mục nếu rỗng).
        private Task<List<ConnectedParentDto>> GetConnectedParentsAsync(int studentId)
            => Task.FromResult(new List<ConnectedParentDto>());
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