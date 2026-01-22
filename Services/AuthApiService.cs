using System.Text;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services
{
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<(LoginResponseDto? data, string? error)> Login(LoginRequestDto request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{ApiConstant.apiBaseUrl}/api/auth/login",
                content
            );

            var resString = await response.Content.ReadAsStringAsync();

            var apiResponse =
                JsonSerializer.Deserialize<ApiResponse<LoginResponseDto>>(resString, _jsonOptions);

            // 🔥 LẤY MESSAGE TỪ API
            if (!response.IsSuccessStatusCode)
            {
                return (null, apiResponse?.Message ?? "Đăng nhập thất bại");
            }

            return (apiResponse!.Data, null);
        }

    }
}
