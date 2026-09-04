namespace ToanHocHay.WebApp.Services.Http
{
    /// <summary>
    /// Gắn <c>X-Client-Key</c> vào mọi request tới Backend = định danh người dùng cuối
    /// (IP thật, ưu tiên <c>X-Forwarded-For</c> nếu WebApp đứng sau proxy).
    ///
    /// Backend dùng header này để phân vùng rate limiter theo từng người dùng thay vì
    /// theo IP của WebApp — nếu không, một người spam login sẽ khoá nhầm tất cả.
    /// Backend chỉ tin header này khi kết nối đến từ proxy tin cậy
    /// (<c>RateLimiting:TrustedProxies</c>).
    /// </summary>
    public sealed class ClientContextHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _http;

        public ClientContextHandler(IHttpContextAccessor http) => _http = http;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var key = ResolveClientKey();
            if (!string.IsNullOrEmpty(key) && !request.Headers.Contains("X-Client-Key"))
                request.Headers.TryAddWithoutValidation("X-Client-Key", key);

            return base.SendAsync(request, ct);
        }

        private string? ResolveClientKey()
        {
            var ctx = _http.HttpContext;
            if (ctx == null) return null;

            // Nếu WebApp đứng sau proxy, X-Forwarded-For chứa IP người dùng thật.
            if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
            {
                var first = xff.ToString().Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(first)) return first;
            }

            return ctx.Connection.RemoteIpAddress?.ToString();
        }
    }
}
