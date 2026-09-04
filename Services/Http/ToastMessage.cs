namespace ToanHocHay.WebApp.Services.Http
{
    /// <summary>
    /// Một thông báo popup (toast) gửi từ server xuống <c>wwwroot/js/toast.js</c>.
    /// Serialize camelCase → <c>window.__THH_TOASTS__</c>.
    /// </summary>
    /// <param name="Message">Nội dung (luôn tiếng Việt).</param>
    /// <param name="Type">"success" | "error" | "warning" | "info".</param>
    /// <param name="ActionLabel">Nhãn nút hành động (tuỳ chọn) — có nút thì toast không tự đóng.</param>
    /// <param name="ActionHref">Đường dẫn nút hành động.</param>
    /// <param name="Title">Tiêu đề in đậm phía trên nội dung (tuỳ chọn).</param>
    public sealed record ToastMessage(
        string Message,
        string Type = "info",
        string? ActionLabel = null,
        string? ActionHref = null,
        string? Title = null);
}
