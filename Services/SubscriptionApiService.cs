// FILE: ToanHocHay.WebApp/Services/SubscriptionApiService.cs
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

        private string? GetToken() =>
            _httpContextAccessor.HttpContext?.Session.GetString("Token")
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("Token")?.Value;

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

        /// <summary>
        /// Lấy subscription hiện tại của học sinh
        /// GET /api/student/{studentId}/subscription/current
        /// Trả về SubscriptionInfoDto (dùng PackageType để map sang PackageId)
        /// </summary>
        public async Task<SubscriptionInfoDto?> GetCurrentSubscriptionAsync(int studentId)
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token)) return null;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Trim());

                // FIX: thêm apiBaseUrl vào URL
                var response = await _httpClient.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/student/{studentId}/subscription/current");

                if (!response.IsSuccessStatusCode) return null;

                var wrapper = await response.Content
                    .ReadFromJsonAsync<ApiResponse<SubscriptionInfoDto>>(_jsonOptions);

                return wrapper?.Success == true ? wrapper.Data : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Kiểm tra trạng thái subscription theo ID
        /// GET /api/Subscription/{id}
        /// </summary>
        public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(int subscriptionId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/Subscription/status/{subscriptionId}");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SubscriptionStatusDto>(json, _jsonOptions);
                Console.WriteLine($"Response is: {result.ToString}");

                return result;
            }
            catch { return null; }
        }
    }
}