using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.DTOs.Notification;
using ToanHocHay.WebApp.Models.DTOs.Payment;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationApiService _api;

        public NotificationController(NotificationApiService api) => _api = api;

        // GET /Notification
        public async Task<IActionResult> Index(int page = 1)
        {
            var r = await _api.GetMineAsync(page);
            if (this.AuthRedirectOrNull(r) is { } redirect) return redirect;

            return View(r.IsSuccess && r.Data != null
                ? r.Data
                : new PagedResultDto<NotificationDto> { Page = page, PageSize = 20 });
        }

        // GET /Notification/UnreadCount — chuông ở layout poll
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var r = await _api.GetUnreadCountAsync();
            return Json(new { count = r.IsSuccess ? r.Data : 0 });
        }

        // GET /Notification/Recent — dropdown chuông
        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var r = await _api.GetMineAsync(1, 6);
            var items = (r.IsSuccess ? r.Data?.Items : null) ?? new List<NotificationDto>();
            return Json(items.Select(n => new
            {
                id = n.NotificationId,
                title = n.Title,
                message = n.Message,
                type = n.NotificationType.ToString(),
                isRead = n.IsRead,
                createdAt = n.CreatedAt
            }));
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var r = await _api.MarkReadAsync(id);
            return Json(new { ok = r.IsSuccess, message = r.IsSuccess ? "Đã đánh dấu là đã đọc." : r.DisplayMessage });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var r = await _api.MarkAllReadAsync();
            return Json(new { ok = r.IsSuccess, message = r.IsSuccess ? "Đã đánh dấu tất cả là đã đọc." : r.DisplayMessage });
        }

        // GET /Notification/Preferences
        [HttpGet]
        public async Task<IActionResult> Preferences()
        {
            var r = await _api.GetPreferencesAsync();
            return View(r.IsSuccess && r.Data != null ? r.Data : new List<NotificationPreferenceDto>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preferences(string ruleKey, bool enabled)
        {
            var r = await _api.SetPreferenceAsync(ruleKey, enabled);
            this.PushToastResult(r, "Đã cập nhật tuỳ chọn thông báo.");
            return RedirectToAction("Preferences");
        }
    }
}
