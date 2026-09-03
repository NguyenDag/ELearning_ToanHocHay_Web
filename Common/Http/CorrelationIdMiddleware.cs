namespace ToanHocHay.WebApp.Common.Http
{
    /// <summary>
    /// Sinh / lan truyền <c>X-Correlation-ID</c> cho mỗi request của WebApp và đẩy vào scope log.
    /// <see cref="ToanHocHay.WebApp.Services.Http.AuthTokenHandler"/> đọc header cùng tên từ
    /// response của Backend và lưu vào <c>HttpContext.Items["ApiCorrelationId"]</c> để đối chiếu.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string ItemKey = "CorrelationId";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var id = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
                     && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString("N")[..12];

            context.Items[ItemKey] = id;
            context.Response.Headers[HeaderName] = id;

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
            {
                await _next(context);
            }
        }
    }

    public static class CorrelationIdHttpContextExtensions
    {
        /// <summary>Correlation-id của chính request WebApp này.</summary>
        public static string? CorrelationId(this HttpContext? ctx)
            => ctx?.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var v) == true ? v as string : null;

        /// <summary>Correlation-id mà Backend API trả về ở lần gọi gần nhất (nếu có).</summary>
        public static string? ApiCorrelationId(this HttpContext? ctx)
            => ctx?.Items.TryGetValue("ApiCorrelationId", out var v) == true ? v as string : null;
    }
}
