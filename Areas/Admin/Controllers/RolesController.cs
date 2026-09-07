using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Admin;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    public class RolesController : AdminBaseController
    {
        private readonly UserAdminApiService _users;

        public RolesController(UserAdminApiService users) => _users = users;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var caps = await _users.GetRoleCapabilitiesAsync();
            return View(caps);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(string email, UserType newRole)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                this.PushToastError("Nhập email người dùng.");
                return RedirectToAction(nameof(Index));
            }

            var user = await _users.GetByEmailForRoleAsync(email.Trim());
            if (user == null)
            {
                this.PushToastError($"Không tìm thấy người dùng với email “{email.Trim()}”.");
                return RedirectToAction(nameof(Index));
            }

            this.PushToastResult(await _users.ChangeRoleAsync(user.UserId, newRole),
                $"Đã đổi vai trò của {user.FullName} sang {AdminRoles.Label(newRole)}. Người dùng cần đăng nhập lại.");
            return RedirectToAction(nameof(Index));
        }
    }
}
