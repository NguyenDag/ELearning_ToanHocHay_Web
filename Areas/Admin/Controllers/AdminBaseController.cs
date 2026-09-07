using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    /// <summary>
    /// Lớp cơ sở cho mọi controller trong khu quản trị. Claim <c>ClaimTypes.Role</c> được gắn bằng
    /// <c>UserType</c> khi đăng nhập (xem <c>AccountController.Login</c>), nên chỉ SystemAdmin vào được.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "SystemAdmin")]
    public abstract class AdminBaseController : Controller
    {
    }
}
