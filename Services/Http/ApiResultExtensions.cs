using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ToanHocHay.WebApp.Common.Http;

namespace ToanHocHay.WebApp.Services.Http
{
    /// <summary>
    /// Bộ chuyển <see cref="ApiResult{T}"/> → thông báo popup (toast) thống nhất cho Controller MVC.
    /// - Qua redirect: <c>PushToast*</c> → <c>TempData["Toast"]</c>.
    /// - Render tại chỗ (không redirect): <c>ShowToast*</c> → <c>ViewData["Toast"]</c>.
    /// <c>Views/Shared/_ToastHost.cshtml</c> đọc cả hai và đẩy xuống <c>toast.js</c>.
    /// Mọi nội dung đều tiếng Việt (lấy từ <see cref="ApiResult{T}.DisplayMessage"/>).
    /// </summary>
    public static class ApiResultExtensions
    {
        public const string ToastKey = "Toast";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static bool SessionExpired<T>(this ApiResult<T> r) => r.IsUnauthorized;

        // ---------- push (redirect) ----------
        public static void PushToast(this Controller c, string message, string type = "info",
            string? actionLabel = null, string? actionHref = null, string? title = null)
            => Append(c.TempData, new ToastMessage(message, type, actionLabel, actionHref, title));

        public static void PushToastSuccess(this Controller c, string message) => c.PushToast(message, "success");
        public static void PushToastError(this Controller c, string message) => c.PushToast(message, "error");
        public static void PushToastWarning(this Controller c, string message) => c.PushToast(message, "warning");
        public static void PushToastInfo(this Controller c, string message) => c.PushToast(message, "info");

        public static void PushToastError<T>(this Controller c, ApiResult<T> r)
            => Append(c.TempData, ToToast(c, r));

        /// <summary>Toast thành công nếu OK, ngược lại toast lỗi (tiếng Việt) — dùng cho action redirect.</summary>
        public static void PushToastResult<T>(this Controller c, ApiResult<T> r, string successMessage)
        {
            if (r.IsSuccess) c.PushToastSuccess(successMessage);
            else Append(c.TempData, ToToast(c, r));
        }

        // ---------- show (render tại chỗ) ----------
        public static void ShowToast(this Controller c, string message, string type = "info",
            string? actionLabel = null, string? actionHref = null, string? title = null)
            => Append(c.ViewData, new ToastMessage(message, type, actionLabel, actionHref, title));

        public static void ShowToastSuccess(this Controller c, string message) => c.ShowToast(message, "success");
        public static void ShowToastError(this Controller c, string message) => c.ShowToast(message, "error");

        public static void ShowToastError<T>(this Controller c, ApiResult<T> r)
            => Append(c.ViewData, ToToast(c, r));

        // ---------- xử lý 401 ----------
        /// <summary>
        /// Nếu 401 → toast "phiên hết hạn" + trả IActionResult về trang đăng nhập; ngược lại null.
        /// </summary>
        public static IActionResult? AuthRedirectOrNull<T>(this Controller c, ApiResult<T> r, string? returnUrl = null)
        {
            if (!r.IsUnauthorized) return null;
            c.PushToast("Phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại.", "warning");
            return c.RedirectToAction("Login", "Account",
                string.IsNullOrEmpty(returnUrl) ? null : new { returnUrl });
        }

        /// <summary>Ánh xạ 400 + Errors từ backend vào ModelState (cho lỗi từng field, nếu có).</summary>
        public static void MergeValidationErrors<T>(this ModelStateDictionary modelState, ApiResult<T> r)
        {
            if (!r.IsValidationError) return;
            foreach (var e in r.Errors) modelState.AddModelError(string.Empty, e);
            if (r.Errors.Count == 0 && !string.IsNullOrEmpty(r.Message))
                modelState.AddModelError(string.Empty, r.Message!);
        }

        // ---------- helpers ----------
        private static ToastMessage ToToast<T>(Controller c, ApiResult<T> r)
        {
            var msg = r.DisplayMessage;
            if (r.Errors.Count > 0)
                msg += " (" + string.Join("; ", r.Errors) + ")";
            if (r.StatusCode >= 500)
            {
                var corr = r.CorrelationId ?? c.HttpContext.ApiCorrelationId();
                if (!string.IsNullOrEmpty(corr)) msg += $" · Mã tra cứu: {corr}";
            }

            if (r.IsUnauthorized)
                return new ToastMessage(msg, "warning", "Đăng nhập lại", "/Account/Login");
            if (r.IsForbidden && LooksLikeUpgrade(r.DisplayMessage))
                return new ToastMessage(msg, "warning", "Nâng cấp gói", "/Package");

            return new ToastMessage(msg, "error");
        }

        private static bool LooksLikeUpgrade(string? m) =>
            m != null && (m.Contains("gói", StringComparison.OrdinalIgnoreCase)
                          || m.Contains("nâng cấp", StringComparison.OrdinalIgnoreCase)
                          || m.Contains("ghi danh", StringComparison.OrdinalIgnoreCase));

        private static void Append(IDictionary<string, object?> bag, ToastMessage toast)
        {
            var list = new List<ToastMessage>();
            if (bag.TryGetValue(ToastKey, out var existing) && existing is string s && !string.IsNullOrWhiteSpace(s))
            {
                try { list = JsonSerializer.Deserialize<List<ToastMessage>>(s, JsonOpts) ?? new(); }
                catch { list = new(); }
            }
            list.Add(toast);
            bag[ToastKey] = JsonSerializer.Serialize(list, JsonOpts);
        }
    }
}
