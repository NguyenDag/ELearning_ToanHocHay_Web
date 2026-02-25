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

        /// <summary>
        /// Thêm mã Token JWT vào Header của yêu cầu để xác thực với Backend.
        /// </summary>
        private void AddAuthHeader()
        {
            // Lấy Token từ Session (được lưu lúc đăng nhập thành công ở AccountController)
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");

            if (!string.IsNullOrEmpty(token))
            {
                // Bắt buộc phải có định dạng "Bearer [Token]" để Backend nhận diện
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Ghi nhật ký Token để kiểm tra (Xem ở cửa sổ Output trong Visual Studio)
                System.Diagnostics.Debug.WriteLine($"[DASHBOARD DEBUG] Token Found: {token.Substring(0, Math.Min(20, token.Length))}...");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DASHBOARD DEBUG] ERROR: No Token found in Session!");
            }
        }

        /// <summary>
        /// Gọi API lấy dữ liệu Dashboard cho học sinh
        /// </summary>
        public async Task<CoreDashboardDto?> GetStudentDashboardAsync(int studentId)
        {
            try
            {
                // Xóa Header cũ và thêm Header mới kèm Token
                _httpClient.DefaultRequestHeaders.Authorization = null;
                AddAuthHeader();

                // Ghi nhật ký URL đang gọi để kiểm tra cổng (Port) có khớp với dự án Control không
                string requestUrl = $"student/{studentId}/dashboard";
                System.Diagnostics.Debug.WriteLine($"[DASHBOARD DEBUG] Calling API: {_httpClient.BaseAddress}{requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                // Lưu mã trạng thái HTTP vào Context để View có thể hiển thị (401, 403, 404, v.v.)
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Items["LastApiStatus"] = (int)response.StatusCode;
                }

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CoreDashboardDto>();
                }

                System.Diagnostics.Debug.WriteLine($"[DASHBOARD DEBUG] API Response Error: {(int)response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                // Lỗi này xảy ra khi không thể kết nối tới máy chủ (Ví dụ: Backend chưa chạy)
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Items["LastApiStatus"] = "CONNECTION_FAILED";
                }
                System.Diagnostics.Debug.WriteLine($"[DASHBOARD DEBUG] HTTP Request Exception: {httpEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Các lỗi logic hoặc hệ thống khác
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Items["LastApiStatus"] = "EXCEPTION";
                }
                System.Diagnostics.Debug.WriteLine($"[DASHBOARD DEBUG] General Exception: {ex.Message}");
                return null;
            }
        }
    }
}