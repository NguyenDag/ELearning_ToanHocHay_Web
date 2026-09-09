using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    /// <summary>
    /// Nền chung cho mọi controller khu quản trị — <b>không</b> gắn phân quyền ở đây.
    /// Vai trò do lớp con quyết định: <see cref="AdminBaseController"/> (chỉ SystemAdmin)
    /// hoặc <see cref="FinanceBaseController"/> (SystemAdmin + FinanceManager).
    /// Claim <c>ClaimTypes.Role</c> được gắn bằng <c>UserType</c> lúc đăng nhập (xem <c>AccountController.Login</c>).
    /// </summary>
    [Area("Admin")]
    public abstract class AdminAreaControllerBase : Controller
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
