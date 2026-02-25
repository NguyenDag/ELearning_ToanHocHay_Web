// ============================================================
// FILE: ToanHocHay.WebApp/Services/DashboardApiService.cs
// Fix deserialize ApiResponse<T> wrapper + subscription info
// ============================================================
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services
{
    public interface IDashboardApiService
    {
        Task<CoreDashboardDto?> GetStudentDashboardAsync(int studentId);
    }

    public class DashboardApiService : IDashboardApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public DashboardApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        private void SetStatus(string status)
        {
            if (_httpContextAccessor.HttpContext != null)
                _httpContextAccessor.HttpContext.Items["LastApiStatus"] = status;
        }

        public async Task<CoreDashboardDto?> GetStudentDashboardAsync(int studentId)
        {
            try
            {
                var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
                if (string.IsNullOrEmpty(token))
                {
                    SetStatus("TOKEN_MISSING_ON_SERVER");
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Trim());

                var response = await _httpClient.GetAsync($"student/{studentId}/dashboard");
                SetStatus(((int)response.StatusCode).ToString());

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    SetStatus($"{(int)response.StatusCode}:{errorBody[..Math.Min(40, errorBody.Length)]}");
                    return null;
                }

                // ✅ FIX CHÍNH: Backend trả ApiResponse<CoreDashboardDto>, không phải CoreDashboardDto thẳng
                var apiResponse = await response.Content
                    .ReadFromJsonAsync<ApiResponse<CoreDashboardDto>>(_jsonOptions);

                if (apiResponse == null || !apiResponse.Success)
                {
                    SetStatus($"API_FAIL:{apiResponse?.Message ?? "null"}");
                    return null;
                }

                return apiResponse.Data;
            }
            catch (Exception ex)
            {
                SetStatus($"ERR:{ex.Message[..Math.Min(30, ex.Message.Length)]}");
                return null;
            }
        }
    }
}