using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ToanHocHay.WebApp.Common.Http;

namespace ToanHocHay.WebApp.Services.Http
{
    /// <summary>
    /// Bộ chuyển <see cref="ApiResult{T}"/> → hành vi UI thống nhất cho Controller MVC.
    /// Quy ước: 401 → về trang đăng nhập; 403 → thông báo "không có quyền / cần nâng gói";
    /// 409/429/400 → thông báo tương ứng; kèm correlation-id để hỗ trợ tra log.
    /// </summary>
    public static class ApiResultExtensions
    {
        public const string TempDataError = "ApiError";
        public const string TempDataSuccess = "ApiSuccess";
        public const string TempDataCorrelation = "ApiErrorCorrelation";

        public static bool SessionExpired<T>(this ApiResult<T> r) => r.IsUnauthorized;

        /// <summary>Đưa lỗi API vào TempData để layout hiển thị ở lần render kế tiếp.</summary>
        public static void PushApiError<T>(this Controller c, ApiResult<T> r)
        {
            c.TempData[TempDataError] = r.DisplayMessage
                + (r.Errors.Count > 0 ? " (" + string.Join("; ", r.Errors) + ")" : "");
            var corr = r.CorrelationId ?? c.HttpContext.ApiCorrelationId();
            if (!string.IsNullOrEmpty(corr))
                c.TempData[TempDataCorrelation] = corr;
        }

        /// <summary>Đưa lỗi API vào ViewBag (khi render ngay view hiện tại, không redirect).</summary>
        public static void SetApiError<T>(this Controller c, ApiResult<T> r)
        {
            c.ViewData["ApiError"] = r.DisplayMessage;
            c.ViewData["ApiErrorCorrelation"] = r.CorrelationId ?? c.HttpContext.ApiCorrelationId();
            foreach (var e in r.Errors)
                c.ModelState.AddModelError(string.Empty, e);
        }

        /// <summary>
        /// Nếu kết quả là lỗi xác thực/uỷ quyền → trả sẵn IActionResult điều hướng phù hợp,
        /// ngược lại trả null để caller tự xử lý phần dữ liệu.
        /// </summary>
        public static IActionResult? AuthRedirectOrNull<T>(this Controller c, ApiResult<T> r,
            string? returnUrl = null)
        {
            if (r.IsUnauthorized)
            {
                c.TempData[TempDataError] = "Phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại.";
                return c.RedirectToAction("Login", "Account",
                    string.IsNullOrEmpty(returnUrl) ? null : new { returnUrl });
            }
            return null;
        }

        /// <summary>Ánh xạ 400 + Errors từ backend vào ModelState.</summary>
        public static void MergeValidationErrors<T>(this ModelStateDictionary modelState, ApiResult<T> r)
        {
            if (!r.IsValidationError) return;
            foreach (var e in r.Errors) modelState.AddModelError(string.Empty, e);
            if (r.Errors.Count == 0 && !string.IsNullOrEmpty(r.Message))
                modelState.AddModelError(string.Empty, r.Message!);
        }
    }
}
