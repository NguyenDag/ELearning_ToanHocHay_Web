using System.Net.Http.Json;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = ApiJson.Options;
        }

        // 1. Đăng nhập
        public async Task<(LoginResponseDto? data, string? error)> Login(LoginRequestDto request)
        {
            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, request, _jsonOptions);
            var resString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponseDto>>(resString, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                return (null, apiResponse?.Message ?? "Đăng nhập thất bại");
            }
            return (apiResponse!.Data, null);
        }

        // 2. Đăng ký
        public async Task<(bool success, string? error)> Register(RegisterRequestDto request)
        {
            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, request, _jsonOptions);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions);

            if (!response.IsSuccessStatusCode || apiResponse == null || !apiResponse.Success)
            {
                return (false, apiResponse?.Message ?? "Đăng ký thất bại");
            }
            return (true, null);
        }

        // 3. Lấy thông tin Profile mới nhất — route đổi /api/user/{id} → /api/users/{id}
        public async Task<UserDto?> GetProfileAsync(int userId)
        {
            try
            {
                var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<UserDto>>(
                    ApiRoutes.Users.ById(userId), _jsonOptions);
                return apiResponse?.Data;
            }
            catch { return null; }
        }

        // 4. Cập nhật thông tin cá nhân — route đổi thành /api/users/update-profile/{id}
        public async Task<ApiResponse<bool>> UpdateProfileAsync(int userId, UpdateProfileDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    ApiRoutes.Users.UpdateProfile(userId), request, _jsonOptions);

                if (!response.IsSuccessStatusCode && response.Content.Headers.ContentLength is null or 0)
                {
                    return ApiResponse<bool>.ErrorResponse("Lỗi server: " + response.StatusCode);
                }

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions);
                return result ?? ApiResponse<bool>.ErrorResponse("Phản hồi lỗi");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse("Lỗi: " + ex.Message);
            }
        }

        // 5. Đổi mật khẩu — route đổi thành /api/auth/change-password (userId lấy từ token).
        // Thành công ⇒ backend thu hồi toàn bộ refresh token + bump SecurityStamp ⇒ phải đăng nhập lại.
        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto request)
        {
            _ = userId; // không còn dùng trên route — giữ chữ ký cho các nơi đang gọi
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    ApiRoutes.Auth.ChangePassword, request, _jsonOptions);

                if (response.Content.Headers.ContentLength == 0)
                {
                    return response.IsSuccessStatusCode
                        ? ApiResponse<bool>.SuccessResponse(true, "Đổi mật khẩu thành công")
                        : ApiResponse<bool>.ErrorResponse("Đổi mật khẩu thất bại");
                }

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions);
                return result ?? ApiResponse<bool>.ErrorResponse("Phản hồi từ server không hợp lệ");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse("Lỗi kết nối: " + ex.Message);
            }
        }
    }
}