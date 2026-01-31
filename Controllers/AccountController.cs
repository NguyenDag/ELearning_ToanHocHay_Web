using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToanHocHay.WebApp.Common;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;
using System.Threading.Tasks;

namespace ToanHocHay.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthApiService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AuthApiService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // ================= LOGIN (GET) =================
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập rồi thì về trang chủ luôn
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.Mode = "login";
            return View();
        }

        // ================= LOGIN (POST) =================
        /// <summary>
        /// Xử lý đăng nhập từ Form. 
        /// Đã đổi tên thành Login (không có Async) để khớp với asp-action="Login"
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Fix CS8130: Khai báo kiểu tường minh cho Tuple để tránh lỗi suy luận kiểu
            (LoginResponseDto? data, string? error) result = await _authService.Login(new LoginRequestDto { Email = email, Password = password });

            var data = result.data;
            var error = result.error;

            if (data != null)
            {
                // 1. Lưu Session (Yêu cầu using Microsoft.AspNetCore.Http)
                // Fix CS1503: Đảm bảo sử dụng phương thức mở rộng SetString chuẩn
                HttpContext.Session.SetString("Token", data.Token ?? "");
                HttpContext.Session.SetInt32("UserId", data.UserId);

                // 2. Thiết lập danh sách thẻ bài (Claims)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, data.UserId.ToString()),
                    new Claim(ClaimTypes.Name, data.FullName ?? ""),
                    new Claim(ClaimTypes.Email, data.Email ?? ""),
                    new Claim(ClaimTypes.Role, data.UserType.ToString())
                };

                // Ghi mã định danh học sinh/phụ huynh vào Cookie
                if (data.StudentId.HasValue)
                {
                    claims.Add(new Claim("StudentId", data.StudentId.Value.ToString()));
                }

                if (data.ParentId.HasValue)
                {
                    claims.Add(new Claim("ParentId", data.ParentId.Value.ToString()));
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Đăng nhập Identity
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = data.TokenExpiration > DateTime.UtcNow ? data.TokenExpiration : DateTimeOffset.UtcNow.AddDays(7)
                });

                return RedirectToAction("Index", "Course");
            }

            // Xử lý khi đăng nhập thất bại
            ViewBag.Error = error ?? "Đăng nhập thất bại.";
            ViewBag.Mode = "login";
            return View();
        }


        // ================= REGISTER (GET) =================
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Mode = "register";
            return View("Login");
        }

        // ================= REGISTER (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, string role)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
                ViewBag.Mode = "register";
                ViewBag.FullName = fullName;
                ViewBag.Email = email;
                return View("Login");
            }

            // Convert role sang Enum tương ứng
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
                ViewBag.FullName = fullName;
                ViewBag.Email = email;
                return View("Login");
            }

            TempData["SuccessMsg"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
            return RedirectToAction("Login");
        }

        // ================= LOGOUT =================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ================= PROFILE =================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            var profile = await _authService.GetProfileAsync(int.Parse(userIdStr));
            if (profile == null) return RedirectToAction("Login");

            return View(profile);
        }
    }
}