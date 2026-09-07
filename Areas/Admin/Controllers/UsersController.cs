using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Admin;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    public class UsersController : AdminBaseController
    {
        private readonly UserAdminApiService _users;

        public UsersController(UserAdminApiService users) => _users = users;

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AdminUserFilter filter)
        {
            filter.Page = AdminPaging.NormalizePage(filter.Page);
            filter.PageSize = AdminPaging.NormalizeSize(filter.PageSize);
            var vm = await _users.ListAsync(filter);
            if (GuardListError(vm) is { } redirect) return redirect;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var u = await _users.GetAsync(id);
            if (u == null)
            {
                this.PushToastError("Không tìm thấy người dùng.");
                return RedirectToAction(nameof(Index));
            }
            return View(u);
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateStaffUserVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStaffUserVm vm)
        {
            if (!AdminRoles.Staff.Contains(vm.UserType))
                ModelState.AddModelError(nameof(vm.UserType), "Vai trò không hợp lệ.");
            if (!ModelState.IsValid) return View(vm);

            var r = await _users.CreateStaffAsync(vm);
            if (r.IsSuccess)
            {
                this.PushToastSuccess("Đã tạo tài khoản nhân sự.");
                return RedirectToAction(nameof(Details), new { id = r.Data!.UserId });
            }
            ModelState.MergeValidationErrors(r);
            this.ShowToastError(r);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                this.PushToastError("Nhập lý do khoá tài khoản.");
                return RedirectToAction(nameof(Details), new { id });
            }
            this.PushToastResult(await _users.LockAsync(id, reason.Trim()), "Đã khoá tài khoản.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            this.PushToastResult(await _users.UnlockAsync(id), "Đã mở khoá tài khoản.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, UserType newRole)
        {
            this.PushToastResult(await _users.ChangeRoleAsync(id, newRole),
                $"Đã đổi vai trò sang {AdminRoles.Label(newRole)}. Người dùng cần đăng nhập lại.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                this.PushToastError("Mật khẩu mới phải có ít nhất 6 ký tự.");
                return RedirectToAction(nameof(Details), new { id });
            }
            this.PushToastResult(await _users.ResetPasswordAsync(id, newPassword),
                "Đã đặt lại mật khẩu. Người dùng cần đăng nhập lại.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail(int id)
        {
            this.PushToastResult(await _users.ConfirmEmailAsync(id), "Đã xác nhận email.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            this.PushToastResult(await _users.DeactivateAsync(id), "Đã vô hiệu hoá tài khoản.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            this.PushToastResult(await _users.ActivateAsync(id), "Đã kích hoạt tài khoản.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _users.DeleteAsync(id);
            this.PushToastResult(r, "Đã xoá tài khoản.");
            return r.IsSuccess ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { id });
        }
    }
}
