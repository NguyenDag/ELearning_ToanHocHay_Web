using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Services.Http;

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
        /// <summary>
        /// Nếu VM báo lỗi nạp dữ liệu: 401 → đưa về trang đăng nhập; lỗi khác → hiện toast
        /// (vẫn render trang để không mất bộ lọc). Trả về IActionResult redirect hoặc null.
        /// </summary>
        protected IActionResult? GuardListError(AdminListVmBase vm)
        {
            if (!vm.Failed) return null;
            if (vm.Unauthorized)
            {
                this.PushToast("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", "warning");
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            this.ShowToastError(vm.Error!);
            return null;
        }
    }
}
