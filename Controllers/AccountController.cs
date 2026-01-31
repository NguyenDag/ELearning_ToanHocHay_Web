using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToanHocHay.WebApp.Common;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;
using System.Text;
using System.Net.Http.Json;

namespace ToanHocHay.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthApiService _authService;
        private readonly ILogger<AccountController> _logger;
        private readonly HttpClient _httpClient;

        public AccountController(AuthApiService authService, ILogger<AccountController> logger, IHttpClientFactory httpClientFactory)
        {
            _authService = authService;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        // ================= LOGIN (GET) =================
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Course");

            ViewBag.Mode = "login";
            return View();
        }

        // ================= LOGIN (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var (data, error) = await _authService.Login(new LoginRequestDto { Email = email, Password = password });

            if (error != null)
            {
                ViewBag.Error = error;
                ViewBag.Mode = "login";
                ViewBag.Email = email;
                return View("Login");
            }

            // Lưu Token vào cả 2 key để các Service cũ/mới đều chạy được
            HttpContext.Session.SetString("Token", data!.Token);
            HttpContext.Session.SetString("JWT", data.Token);
            HttpContext.Session.SetInt32("UserId", data.UserId);
            HttpContext.Session.SetString("UserFullName", data.FullName ?? "");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, data.UserId.ToString()),
                new Claim(ClaimTypes.Name, data.FullName ?? ""),
                new Claim(ClaimTypes.Email, data.Email ?? ""),
                new Claim(ClaimTypes.Role, data.UserType.ToString())
            };

            if (data.StudentId.HasValue) claims.Add(new Claim("StudentId", data.StudentId.Value.ToString()));
            if (data.ParentId.HasValue) claims.Add(new Claim("ParentId", data.ParentId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

            _logger.LogInformation("User {Email} đăng nhập thành công.", email);
            return RedirectToAction("Index", "Course");
        }

        // ================= REGISTER =================
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Mode = "register";
            return View("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, string role)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
                ViewBag.Mode = "register";
                return View("Login");
            }

            UserType userType = (role?.ToLower() == "student") ? UserType.Student : UserType.Parent;

            var request = new RegisterRequestDto
            {
                FullName = fullName,
                Email = email,
                Password = password,
                ConfirmPassword = password,
                UserType = userType,
                GradeLevel = (userType == UserType.Student) ? 6 : null
            };

            var (success, error) = await _authService.Register(request);

            if (!success)
            {
                ViewBag.Error = error ?? "Đăng ký không thành công.";
                ViewBag.Mode = "register";
                return View("Login");
            }

            TempData["SuccessMsg"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
            return RedirectToAction("Login");
        }

        // ================= PROFILE & UPDATE =================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            var profile = await _authService.GetProfileAsync(int.Parse(userIdStr));
            if (profile == null) return RedirectToAction("Login");

            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto model)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            var response = await _authService.UpdateProfileAsync(int.Parse(userIdStr), model);

            if (response.Success)
            {
                // Cập nhật lại Identity để UI (Header) hiển thị tên mới ngay lập tức
                var identity = (ClaimsIdentity)User.Identity!;
                var nameClaim = identity.FindFirst(ClaimTypes.Name);
                if (nameClaim != null) identity.RemoveClaim(nameClaim);
                identity.AddClaim(new Claim(ClaimTypes.Name, model.FullName));

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });

                TempData["Success"] = "Cập nhật thông tin thành công!";
            }
            else
            {
                ViewBag.Error = response.Message;
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            var response = await _authService.ChangePasswordAsync(int.Parse(userIdStr), model);

            if (response.Success)
            {
                TempData["Success"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }

            ViewBag.Error = response.Message;
            var userProfile = await _authService.GetProfileAsync(int.Parse(userIdStr));
            return View("Profile", userProfile);
        }

        // ================= LOGOUT =================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ================= EMAIL CONFIRMATION =================
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return View("ConfirmEmailFailed");

            var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/auth/confirm-email?token={token}");

            if (!response.IsSuccessStatusCode) return View("ConfirmEmailFailed");

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            if (result == null || !result.Success) return View("ConfirmEmailFailed");

            return View("ConfirmEmailSuccess");
        }

        [HttpPost]
        public async Task<IActionResult> ResendConfirmationEmail(string email)
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiConstant.apiBaseUrl}/api/auth/resend-confirmation-email", new { Email = email });

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMsg"] = "Email xác nhận mới đã được gửi!";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "Email không tồn tại hoặc có lỗi xảy ra.";
            return View("ConfirmEmailFailed");
        }
    }
}