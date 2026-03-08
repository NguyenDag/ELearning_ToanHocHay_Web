// FILE: ToanHocHay.WebApp/Services/DashboardApiService.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services
{
    public interface IDashboardApiService
    {
        Task<CoreDashboardDto?> GetStudentDashboardAsync(int studentId);
        Task<List<ChapterScoreDto>?> GetChapterScoreComparisonAsync(int studentId);
        Task<AIInsightResponse?> GetAIInsightAsync(int studentId);
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

        private string? GetToken()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) return null;

            var sessionToken = ctx.Session.GetString("Token")
                            ?? ctx.Session.GetString("JWT");
            if (!string.IsNullOrEmpty(sessionToken)) return sessionToken;

            return ctx.User.FindFirst("Token")?.Value
                ?? ctx.User.FindFirst("jwt")?.Value;
        }

        public async Task<CoreDashboardDto?> GetStudentDashboardAsync(int studentId)
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    SetStatus("TOKEN_MISSING");
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Trim());

                // FIX: thêm ApiConstant.apiBaseUrl — trước đây thiếu nên request fail im lặng
                var response = await _httpClient.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/student/{studentId}/dashboard");

                SetStatus(((int)response.StatusCode).ToString());

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    SetStatus($"{(int)response.StatusCode}:{errorBody[..Math.Min(40, errorBody.Length)]}");
                    return null;
                }

                var apiResponse = await response.Content
                    .ReadFromJsonAsync<ApiResponse<CoreDashboardDto>>(_jsonOptions);

                if (apiResponse == null || !apiResponse.Success)
                {
                    SetStatus($"API_FAIL:{apiResponse?.Message ?? "null"}");
                    return null;
                }

                _httpContextAccessor.HttpContext?.Session.SetString("Token", token);
                return apiResponse.Data;
            }
            catch (Exception ex)
            {
                SetStatus($"ERR:{ex.Message[..Math.Min(30, ex.Message.Length)]}");
                return null;
            }
        }

        public async Task<List<ChapterScoreDto>?> GetChapterScoreComparisonAsync(int studentId)
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token)) return null;
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Trim());
                var response = await _httpClient.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/student/{studentId}/dashboard/chapter-score-comparison");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<ChapterScoreDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result;
            }
            catch { return null; }
        }

        public async Task<AIInsightResponse?> GetAIInsightAsync(int studentId)
        {
            try
            {
                var token = GetToken();
                if (string.IsNullOrEmpty(token)) return null;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Trim());

                var response = await _httpClient.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/student/{studentId}/dashboard/ai-insights");

                if (!response.IsSuccessStatusCode) return null;

                var result = await response.Content.ReadFromJsonAsync<AIInsightResponse>(_jsonOptions);
                return result;
            }
            catch { return null; }
        }
    }

    public class ChapterScoreDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = "";
        public decimal AverageScore { get; set; }
    }
}