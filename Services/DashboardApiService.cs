using System.Net.Http.Headers;
using System.Net.Http.Json;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Định nghĩa interface cho dịch vụ Dashboard
    /// </summary>
    public interface IDashboardApiService
    {
        Task<CoreDashboardDto?> GetStudentDashboardAsync(int studentId);
    }

    /// <summary>
    /// Dịch vụ kết nối API Dashboard với cơ chế gỡ lỗi chuyên sâu.
    /// </summary>
    public class DashboardApiService : IDashboardApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
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

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

                // Gọi API
                var response = await _httpClient.GetAsync($"student/{studentId}/dashboard");

                SetStatus(((int)response.StatusCode).ToString());

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CoreDashboardDto>();
                }
                return null;
            }
            catch (Exception ex)
            {
                // Nếu bị N/A trên server, thông báo này sẽ cho biết do lỗi mạng hay lỗi Code
                SetStatus($"SERVER_ERR: {ex.Message.Substring(0, Math.Min(20, ex.Message.Length))}");
                return null;
            }
        }
    }
}