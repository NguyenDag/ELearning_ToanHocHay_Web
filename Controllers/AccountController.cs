// FILE: ToanHocHay.WebApp/Controllers/AccountController.cs
// Chỉ thay phần Login POST — phần còn lại giữ nguyên

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToanHocHay.WebApp.Common;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;
using System.Text;
using System.Net.Http.Json;

namespace ToanHocHay.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthApiService _authService;
        private readonly ILogger<AccountController> _logger;
        private readonly HttpClient _httpClient;
        private readonly ITokenStore _tokenStore;

        public AccountController(
            AuthApiService authService,
            ILogger<AccountController> logger,
            IHttpClientFactory httpClientFactory,
            ITokenStore tokenStore)
        {
            _authService = authService;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _tokenStore = tokenStore;
        }

        /// <summary>Bậc gói → số cũ (0=Free,1=Standard,2=Premium/Yearly) cho claim "PackageType" — giữ
        /// tương thích các chỗ đang đọc claim này (Exam...). Sẽ bỏ khi các đợt sau dùng "PackageTier".</summary>
        private static int LegacyPackageLevel(PackageTier tier) => tier switch
        {
            PackageTier.Standard => 1,
            PackageTier.Premium => 2,
            PackageTier.Yearly => 2,
            _ => 0
        };

        // ================= LOGIN (GET) =================
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole();

            ViewBag.Mode = "login";
            return View();
        }

        // ================= LOGIN (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
                _tokenStore.Clear();
                Response.Cookies.Delete("ToanHocHay_Auth_Cookie");
            }
            var (data, error) = await _authService.Login(new LoginRequestDto { Email = email, Password = password });

            if (error != null)
            {
                ViewBag.Error = error;
                ViewBag.Mode = "login";
                ViewBag.Email = email;
                return View("Login");
            }

            // Lưu cặp access + refresh token (AuthTokenHandler sẽ tự refresh khi hết hạn).
            _tokenStore.Save(
                data!.Token,
                data.TokenExpiration == default ? DateTime.UtcNow.AddMinutes(25) : data.TokenExpiration,
                data.RefreshToken,
                data.RefreshTokenExpiration);

            HttpContext.Session.SetInt32("UserId", data.UserId);
            HttpContext.Session.SetString("UserFullName", data.FullName ?? "");

            var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, data.UserId.ToString()),
    new Claim(ClaimTypes.Name, data.FullName ?? ""),
    new Claim(ClaimTypes.Email, data.Email ?? ""),
    new Claim(ClaimTypes.Role, data.UserType.ToString()),
    new Claim("Token", data.Token),
    new Claim("PackageTier", data.PackageTier.ToString()),
    new Claim("PackageType", LegacyPackageLevel(data.PackageTier).ToString()), // tương thích cũ
};

            if (data.StudentId.HasValue) claims.Add(new Claim("StudentId", data.StudentId.Value.ToString()));
            if (data.ParentId.HasValue) claims.Add(new Claim("ParentId", data.ParentId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

            _logger.LogInformation("User {Email} đăng nhập thành công. Role: {Role}", email, data.UserType);
            var expiryMs = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds();
            Response.Cookies.Append("session_expiry_hint", expiryMs.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = false, // FE cần đọc được
                SameSite = SameSiteMode.Lax
            });
            
            // Set active user hint để đồng bộ multi-tab
            Response.Cookies.Append("active_user_hint", data.UserId.ToString(), new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            // FIX: redirect theo role
            return data.UserType switch
            {
                UserType.Parent => RedirectToAction("Dashboard", "Parent"),
                UserType.Student => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // Helper: redirect theo role của user hiện tại
        private IActionResult RedirectByRole()
        {
            if (User.IsInRole("Parent"))
                return RedirectToAction("Dashboard", "Parent");
            if (User.IsInRole("Student"))
                return RedirectToAction("Index", "Dashboard");
            return RedirectToAction("Index", "Home");
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
                ViewBag.Role = role;
                ViewBag.FullName = fullName;
                ViewBag.Email = email;
                return View("Login");
            }

            TempData["SuccessMsg"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.";
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
                // Backend thu hồi toàn bộ refresh token + bump SecurityStamp → token cũ chết ngay.
                await _authService.LogoutAsync(_tokenStore.RefreshToken);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _tokenStore.Clear();
                HttpContext.Session.Clear();
                Response.Cookies.Delete("ToanHocHay_Auth_Cookie");

                TempData["SuccessMsg"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = response.Message;
            var userProfile = await _authService.GetProfileAsync(int.Parse(userIdStr));
            return View("Profile", userProfile);
        }

        // ================= LOGOUT =================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Thu hồi refresh token phía backend trước khi xoá phiên.
            await _authService.LogoutAsync(_tokenStore.RefreshToken);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _tokenStore.Clear();
            HttpContext.Session.Clear();
            Response.Cookies.Delete("ToanHocHay_Auth_Cookie");
            return RedirectToAction("Login", "Account");
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
            var response = await _httpClient.PostAsJsonAsync(
                $"{ApiConstant.apiBaseUrl}/api/{ApiRoutes.Auth.ResendConfirmation}",
                new { Email = email });

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