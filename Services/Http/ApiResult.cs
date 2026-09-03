using System.Net;

namespace ToanHocHay.WebApp.Services.Http
{
    /// <summary>
    /// Kết quả một lời gọi API đã được chuẩn hoá: giữ lại mã HTTP thật để tầng trên
    /// phân biệt 401 / 403 / 404 / 409 / 429 thay vì chỉ biết "thành công / thất bại".
    /// Backend nay trả đúng mã HTTP + vỏ <c>ApiResponse&lt;T&gt;</c>.
    /// </summary>
    public class ApiResult<T>
    {
        public int StatusCode { get; init; }
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public string? Message { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

        /// <summary>Correlation-id server trả về (header X-Correlation-ID) — để đối chiếu log.</summary>
        public string? CorrelationId { get; init; }

        public bool IsUnauthorized => StatusCode == (int)HttpStatusCode.Unauthorized;      // 401
        public bool IsForbidden => StatusCode == (int)HttpStatusCode.Forbidden;            // 403
        public bool IsNotFound => StatusCode == (int)HttpStatusCode.NotFound;              // 404
        public bool IsConflict => StatusCode == (int)HttpStatusCode.Conflict;              // 409
        public bool IsTooManyRequests => StatusCode == 429;
        public bool IsValidationError => StatusCode == (int)HttpStatusCode.BadRequest;     // 400

        /// <summary>Thông điệp gọn để hiển thị cho người dùng.</summary>
        public string DisplayMessage => !string.IsNullOrWhiteSpace(Message)
            ? Message!
            : StatusCode switch
            {
                401 => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                403 => "Bạn không có quyền thực hiện thao tác này.",
                404 => "Không tìm thấy dữ liệu.",
                409 => "Dữ liệu đã tồn tại hoặc đã được xử lý.",
                429 => "Bạn thao tác quá nhanh hoặc đã hết lượt. Vui lòng thử lại sau.",
                >= 500 => "Máy chủ đang gặp sự cố. Vui lòng thử lại sau.",
                _ => "Đã xảy ra lỗi khi kết nối máy chủ."
            };

        public static ApiResult<T> Fail(int status, string? message = null, IReadOnlyList<string>? errors = null,
            string? correlationId = null) => new()
        {
            StatusCode = status,
            IsSuccess = false,
            Message = message,
            Errors = errors ?? Array.Empty<string>(),
            CorrelationId = correlationId
        };

        public static ApiResult<T> Ok(T? data, int status = 200, string? message = null, string? correlationId = null) => new()
        {
            StatusCode = status,
            IsSuccess = true,
            Data = data,
            Message = message,
            CorrelationId = correlationId
        };
    }

    /// <summary>Bản không có payload — cho các thao tác POST/PUT/DELETE chỉ cần biết thành/bại.</summary>
    public sealed class ApiResult : ApiResult<object>
    {
        public static ApiResult From<T>(ApiResult<T> r) => new()
        {
            StatusCode = r.StatusCode,
            IsSuccess = r.IsSuccess,
            Message = r.Message,
            Errors = r.Errors,
            CorrelationId = r.CorrelationId
        };
    }
}
