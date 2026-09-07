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
        private readonly SubscriptionApiService _subscriptions;

        public AccountController(
            AuthApiService authService,
            ILogger<AccountController> logger,
            IHttpClientFactory httpClientFactory,
            ITokenStore tokenStore,
            SubscriptionApiService subscriptions)
        {
            _authService = authService;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _tokenStore = tokenStore;
            _subscriptions = subscriptions;
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
                this.ShowToastError(error);
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
                this.ShowToastError("Mật khẩu phải có ít nhất 6 ký tự.");
                ViewBag.Mode = "register";
                ViewBag.FullName = fullName;
                ViewBag.Email = email;
                ViewBag.Role = role;
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
                this.ShowToastError(error ?? "Đăng ký không thành công.");
                ViewBag.Mode = "register";
                ViewBag.Role = role;
                ViewBag.FullName = fullName;
                ViewBag.Email = email;
                return View("Login");
            }

            this.PushToastSuccess("Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.");
            return RedirectToAction("Login");
        }

        // ================= PROFILE & UPDATE =================
        [HttpGet]
        public IActionResult Profile() => RedirectToAction("Profile", "Student");

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

                this.PushToastSuccess("Cập nhật thông tin thành công!");
            }
            else
            {
                this.PushToastError(string.IsNullOrWhiteSpace(response.Message)
                    ? "Không cập nhật được thông tin. Vui lòng thử lại." : response.Message);
            }
            return RedirectToAction("Profile", "Student");
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

                this.PushToastSuccess("Đổi mật khẩu thành công! Vui lòng đăng nhập lại.");
                return RedirectToAction("Login");
            }

            this.PushToastError(string.IsNullOrWhiteSpace(response.Message)
                ? "Không đổi được mật khẩu. Vui lòng kiểm tra lại mật khẩu hiện tại." : response.Message);
            return RedirectToAction("Profile", "Student");
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

        // ================= ĐỒNG BỘ GÓI SAU KHI THANH TOÁN =================
        // Sau khi mua gói, claim "PackageTier" trong cookie vẫn cũ (30' theo access token).
        // Gọi endpoint này (từ trang thanh toán thành công) để cập nhật claim ngay, không cần đăng nhập lại.
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> SyncPackage()
        {
            var info = await _subscriptions.GetMySubscriptionAsync();
            var tier = info?.PackageTier ?? PackageTier.Free;

            var identity = (ClaimsIdentity)User.Identity!;
            foreach (var name in new[] { "PackageTier", "PackageType" })
            {
                var old = identity.FindFirst(name);
                if (old != null) identity.RemoveClaim(old);
            }
            identity.AddClaim(new Claim("PackageTier", tier.ToString()));
            identity.AddClaim(new Claim("PackageType", LegacyPackageLevel(tier).ToString()));

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return Json(new { ok = true, tier = tier.ToString(), packageName = info?.PackageName ?? "Free" });
        }

        // ================= QUÊN / ĐẶT LẠI MẬT KHẨU =================

        // GET /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectByRole();
            return View();
        }

        // POST /Account/ForgotPassword — backend không lộ email có tồn tại hay không.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                this.ShowToastError("Vui lòng nhập email.");
                return View();
            }

            await _authService.ForgotPasswordAsync(email.Trim());

            // Luôn báo thành công (chống dò email tồn tại).
            ViewBag.Sent = true;
            ViewBag.Email = email.Trim();
            return View();
        }

        // GET /reset-password?token=...  (khớp liên kết trong email backend) và /Account/ResetPassword
        [HttpGet("/reset-password")]
        [HttpGet("Account/ResetPassword")]
        public IActionResult ResetPassword(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                this.ShowToastError("Liên kết đặt lại mật khẩu không hợp lệ.");
                ViewBag.Invalid = true;
            }
            ViewBag.Token = token;
            return View();
        }

        // POST /Account/ResetPassword
        [HttpPost("/reset-password")]
        [HttpPost("Account/ResetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            ViewBag.Token = token;

            if (string.IsNullOrWhiteSpace(token))
            {
                this.ShowToastError("Liên kết đặt lại mật khẩu không hợp lệ.");
                ViewBag.Invalid = true;
                return View();
            }
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                this.ShowToastError("Mật khẩu mới phải có ít nhất 6 ký tự.");
                return View();
            }
            if (newPassword != confirmPassword)
            {
                this.ShowToastError("Mật khẩu xác nhận không khớp.");
                return View();
            }

            var result = await _authService.ResetPasswordAsync(token, newPassword);
            if (result.Success)
            {
                this.PushToastSuccess("Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.");
                return RedirectToAction("Login");
            }

            this.ShowToastError(string.IsNullOrWhiteSpace(result.Message)
                ? "Không đặt lại được mật khẩu. Liên kết có thể đã hết hạn hoặc đã dùng." : result.Message);
            return View();
        }

        // ================= EMAIL CONFIRMATION =================
        // GET /Account/ConfirmEmail?token=...  (khớp liên kết trong email backend)
        [HttpGet("/xac-thuc-email")]
        [HttpGet("Account/ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return View("ConfirmEmailFailed");

            try
            {
                var response = await _httpClient.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/auth/confirm-email?token={Uri.EscapeDataString(token)}");

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();

                if (result?.Success == true) return View("ConfirmEmailSuccess");

                // Liên kết quá hạn có trang riêng, kèm nút gửi lại email.
                if ((result?.Message ?? string.Empty).Contains("hết hạn", StringComparison.OrdinalIgnoreCase))
                    return View("ConfirmEmailExpired");

                return View("ConfirmEmailFailed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConfirmEmail: lỗi khi gọi API xác thực email");
                return View("ConfirmEmailFailed");
            }
        }

        [HttpGet]
        public IActionResult ResendConfirmationEmail() => View();

        [HttpPost]
        public async Task<IActionResult> ResendConfirmationEmail(string email)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{ApiConstant.apiBaseUrl}/api/{ApiRoutes.Auth.ResendConfirmation}",
                new { Email = email });

            if (response.IsSuccessStatusCode)
            {
                this.PushToastSuccess("Nếu email hợp lệ và chưa xác nhận, chúng tôi đã gửi lại email xác nhận.");
                return RedirectToAction("Login");
            }

            this.PushToastError((int)response.StatusCode == 429
                ? "Bạn yêu cầu quá nhanh. Vui lòng thử lại sau ít phút."
                : "Không gửi lại được email xác nhận. Vui lòng thử lại sau.");
            return RedirectToAction("Login");
        }
    }
}