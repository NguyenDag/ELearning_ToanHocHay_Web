using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ToanHocHay.WebApp.Services.Http
{
    /// <summary>
    /// Tự động gắn <c>Authorization: Bearer</c> vào mọi request tới Backend API và
    /// tự làm mới access token khi hết hạn.
    ///
    /// Backend rút access token còn 30 phút và có refresh token thật
    /// (<c>POST /api/auth/refresh-token</c>, xoay vòng mỗi lần dùng, phát hiện replay).
    /// Nếu refresh thất bại (token bị thu hồi do đổi mật khẩu / bị khoá / replay) →
    /// xoá token khỏi session; request trả 401 để tầng trên đưa người dùng về trang đăng nhập.
    /// </summary>
    public class AuthTokenHandler : DelegatingHandler
    {
        // Client "thô" chỉ để gọi refresh — KHÔNG gắn handler này để tránh đệ quy.
        public const string RawClientName = "ApiRaw";

        private static readonly SemaphoreSlim RefreshLock = new(1, 1);

        private readonly ITokenStore _tokens;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IHttpContextAccessor _httpContext;
        private readonly ILogger<AuthTokenHandler> _logger;

        public AuthTokenHandler(
            ITokenStore tokens,
            IHttpClientFactory clientFactory,
            IHttpContextAccessor httpContext,
            ILogger<AuthTokenHandler> logger)
        {
            _tokens = tokens;
            _clientFactory = clientFactory;
            _httpContext = httpContext;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            // Đệm nội dung để có thể gửi lại nếu phải retry sau khi refresh.
            if (request.Content != null)
                await request.Content.LoadIntoBufferAsync();

            // Refresh chủ động khi access token sắp hết hạn.
            var expires = _tokens.AccessTokenExpiresAtUtc;
            if (!string.IsNullOrEmpty(_tokens.RefreshToken)
                && expires.HasValue
                && expires.Value <= DateTime.UtcNow.AddSeconds(60))
            {
                await TryRefreshAsync(ct);
            }

            ApplyBearer(request);
            var response = await base.SendAsync(request, ct);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                CaptureCorrelationId(response);
                return response;
            }

            // 401 → thử refresh một lần rồi gửi lại.
            if (string.IsNullOrEmpty(_tokens.RefreshToken))
                return response;

            var refreshed = await TryRefreshAsync(ct);
            if (!refreshed)
                return response;

            response.Dispose();
            var retry = await CloneAsync(request);
            ApplyBearer(retry);
            var retryResponse = await base.SendAsync(retry, ct);
            CaptureCorrelationId(retryResponse);
            return retryResponse;
        }

        private void ApplyBearer(HttpRequestMessage request)
        {
            var token = _tokens.AccessToken;
            request.Headers.Authorization = string.IsNullOrEmpty(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task<bool> TryRefreshAsync(CancellationToken ct)
        {
            var refreshToken = _tokens.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken)) return false;

            await RefreshLock.WaitAsync(ct);
            try
            {
                // Request song song khác có thể đã refresh xong trong lúc chờ lock.
                var current = _tokens.RefreshToken;
                if (!string.IsNullOrEmpty(current) && !ReferenceEquals(current, refreshToken)
                    && current != refreshToken)
                {
                    var exp = _tokens.AccessTokenExpiresAtUtc;
                    if (exp.HasValue && exp.Value > DateTime.UtcNow.AddSeconds(30))
                        return true;
                }

                var raw = _clientFactory.CreateClient(RawClientName);
                using var resp = await raw.PostAsJsonAsync(
                    Common.Constants.ApiRoutes.Auth.RefreshToken,
                    new { refreshToken = _tokens.RefreshToken },
                    ApiJson.Options, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Refresh token thất bại ({Status}) — buộc đăng nhập lại.", (int)resp.StatusCode);
                    _tokens.Clear();
                    return false;
                }

                var body = await resp.Content.ReadAsStringAsync(ct);
                var pair = ExtractPair(body);
                if (pair == null || string.IsNullOrEmpty(pair.Token))
                {
                    _tokens.Clear();
                    return false;
                }

                _tokens.UpdateAccess(pair.Token, pair.TokenExpiration, pair.RefreshToken, pair.RefreshTokenExpiration);
                _logger.LogInformation("Đã làm mới access token, hết hạn lúc {Exp:o}.", pair.TokenExpiration);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi làm mới token.");
                return false;
            }
            finally
            {
                RefreshLock.Release();
            }
        }

        private static TokenPair? ExtractPair(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            // Vỏ ApiResponse<TokenPairDto>: { success, data: { token, tokenExpiration, refreshToken, refreshTokenExpiration } }
            var el = root.TryGetProperty("data", out var d) ? d
                   : root.TryGetProperty("Data", out var d2) ? d2
                   : root;
            if (el.ValueKind != JsonValueKind.Object) return null;

            string? S(params string[] names)
            {
                foreach (var n in names)
                    if (el.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String)
                        return v.GetString();
                return null;
            }
            DateTime D(params string[] names)
            {
                foreach (var n in names)
                    if (el.TryGetProperty(n, out var v) && v.TryGetDateTime(out var dt))
                        return dt.ToUniversalTime();
                return DateTime.UtcNow.AddMinutes(25);
            }

            var token = S("token", "Token");
            if (string.IsNullOrEmpty(token)) return null;

            return new TokenPair
            {
                Token = token!,
                TokenExpiration = D("tokenExpiration", "TokenExpiration"),
                RefreshToken = S("refreshToken", "RefreshToken"),
                RefreshTokenExpiration = el.TryGetProperty("refreshTokenExpiration", out var re) && re.TryGetDateTime(out var rdt)
                    ? rdt.ToUniversalTime()
                    : el.TryGetProperty("RefreshTokenExpiration", out var re2) && re2.TryGetDateTime(out var rdt2)
                        ? rdt2.ToUniversalTime()
                        : null
            };
        }

        private void CaptureCorrelationId(HttpResponseMessage response)
        {
            if (_httpContext.HttpContext == null) return;
            if (response.Headers.TryGetValues("X-Correlation-ID", out var vals))
                _httpContext.HttpContext.Items["ApiCorrelationId"] = vals.FirstOrDefault();
        }

        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri) { Version = req.Version };

            if (req.Content != null)
            {
                var bytes = await req.Content.ReadAsByteArrayAsync();
                var content = new ByteArrayContent(bytes);
                foreach (var h in req.Content.Headers)
                    content.Headers.TryAddWithoutValidation(h.Key, h.Value);
                clone.Content = content;
            }

            foreach (var h in req.Headers)
            {
                if (string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)) continue;
                clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
            return clone;
        }

        private sealed class TokenPair
        {
            public string Token { get; set; } = "";
            public DateTime TokenExpiration { get; set; }
            public string? RefreshToken { get; set; }
            public DateTime? RefreshTokenExpiration { get; set; }
        }
    }
}
