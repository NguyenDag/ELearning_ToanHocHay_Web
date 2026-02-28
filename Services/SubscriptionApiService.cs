using System.Net.Http.Headers;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services
{
    public class SubscriptionApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public SubscriptionApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        private void AddAuthHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Tạo subscription + lấy QR URL
        /// POST /api/Subscription
        /// </summary>
        public async Task<CreateSubscriptionResultDto?> CreateSubscriptionAsync(int studentId, int packageId, decimal amount)
        {
            try
            {
                AddAuthHeader();
                var payload = new { StudentId = studentId, PackageId = packageId, AmountPaid = amount };
                var response = await _httpClient.PostAsJsonAsync($"{ApiConstant.apiBaseUrl}/api/Subscription", payload);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CreateSubscriptionResultDto>(json, _jsonOptions);
            }
            catch { return null; }
        }
        public async Task<CurrentSubscriptionDto?> GetCurrentSubscriptionAsync(int studentId)
        {
            try
            {
                var token = _httpContextAccessor.HttpContext?.Session.GetString("Token")
                         ?? _httpContextAccessor.HttpContext?.User.FindFirst("Token")?.Value;
                if (string.IsNullOrEmpty(token)) return null;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Trim());

                var response = await _httpClient.GetAsync($"student/{studentId}/subscription/current");
                if (!response.IsSuccessStatusCode) return null;

                var wrapper = await response.Content
                    .ReadFromJsonAsync<ApiResponse<CurrentSubscriptionDto>>(_jsonOptions);
                return wrapper?.Success == true ? wrapper.Data : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Kiểm tra trạng thái subscription
        /// GET /api/Subscription/{id}
        /// </summary>
        public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(int subscriptionId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/Subscription/{subscriptionId}");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<SubscriptionStatusDto>>(json, _jsonOptions);
                return result?.Data;
            }
            catch { return null; }
        }
    }
}