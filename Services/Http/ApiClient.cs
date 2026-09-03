using System.Net.Http.Json;
using System.Text.Json;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services.Http
{
    /// <summary>
    /// Bọc <see cref="HttpClient"/> để mọi service gọi Backend theo một kiểu duy nhất:
    /// - luôn giải mã vỏ <c>ApiResponse&lt;T&gt;</c>
    /// - giữ lại mã HTTP thật trong <see cref="ApiResult{T}"/> (401/403/404/409/429...)
    /// - dùng chung <see cref="ApiJson.Options"/> (đã có JsonStringEnumConverter)
    ///
    /// <c>HttpClient.BaseAddress</c> đã là <c>{apiBaseUrl}/api/</c> nên chỉ truyền path
    /// tương đối lấy từ <see cref="Common.Constants.ApiRoutes"/>.
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient http, ILogger<ApiClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public HttpClient Raw => _http;

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
            => SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path), path, ct);

        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => SendAsync<T>(Build(HttpMethod.Post, path, body), path, ct);

        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => AsUnit(PostAsync<object>(path, body, ct));

        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => SendAsync<T>(Build(HttpMethod.Put, path, body), path, ct);

        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => AsUnit(PutAsync<object>(path, body, ct));

        public Task<ApiResult<T>> PatchAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => SendAsync<T>(Build(HttpMethod.Patch, path, body), path, ct);

        public Task<ApiResult<T>> DeleteAsync<T>(string path, CancellationToken ct = default)
            => SendAsync<T>(new HttpRequestMessage(HttpMethod.Delete, path), path, ct);

        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => AsUnit(DeleteAsync<object>(path, ct));

        private static HttpRequestMessage Build(HttpMethod method, string path, object? body)
        {
            var req = new HttpRequestMessage(method, path);
            if (body != null)
                req.Content = JsonContent.Create(body, options: ApiJson.Options);
            return req;
        }

        private static async Task<ApiResult> AsUnit<T>(Task<ApiResult<T>> task)
            => ApiResult.From(await task);

        private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, string path, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.SendAsync(request, ct);
                var status = (int)resp.StatusCode;
                var correlationId = resp.Headers.TryGetValues("X-Correlation-ID", out var cid)
                    ? cid.FirstOrDefault()
                    : null;

                var raw = await resp.Content.ReadAsStringAsync(ct);

                if (string.IsNullOrWhiteSpace(raw))
                {
                    return resp.IsSuccessStatusCode
                        ? ApiResult<T>.Ok(default, status, correlationId: correlationId)
                        : ApiResult<T>.Fail(status, correlationId: correlationId);
                }

                ApiResponse<T>? envelope = null;
                try
                {
                    envelope = JsonSerializer.Deserialize<ApiResponse<T>>(raw, ApiJson.Options);
                }
                catch (JsonException)
                {
                    // Không phải vỏ ApiResponse (ví dụ ProblemDetails ở 500) → thử lấy "message"/"title".
                    var fallback = TryReadMessage(raw);
                    return resp.IsSuccessStatusCode
                        ? ApiResult<T>.Ok(SafeDeserialize<T>(raw), status, correlationId: correlationId)
                        : ApiResult<T>.Fail(status, fallback, correlationId: correlationId);
                }

                if (envelope == null)
                    return ApiResult<T>.Fail(status, correlationId: correlationId);

                var success = resp.IsSuccessStatusCode && envelope.Success;
                return new ApiResult<T>
                {
                    StatusCode = status,
                    IsSuccess = success,
                    Data = envelope.Data,
                    Message = envelope.Message,
                    Errors = envelope.Errors ?? new List<string>(),
                    CorrelationId = correlationId
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "Không kết nối được API: {Path}", path);
                return ApiResult<T>.Fail(0, "Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.");
            }
        }

        private static T? SafeDeserialize<T>(string raw)
        {
            try { return JsonSerializer.Deserialize<T>(raw, ApiJson.Options); }
            catch { return default; }
        }

        private static string? TryReadMessage(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                foreach (var key in new[] { "message", "Message", "title", "detail", "error" })
                    if (doc.RootElement.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                        return v.GetString();
            }
            catch { /* ignore */ }
            return null;
        }
    }
}
