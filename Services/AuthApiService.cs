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
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, request, _jsonOptions);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return (null, "Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.");
            }

            var apiResponse = await TryReadEnvelopeAsync<LoginResponseDto>(response);

            if (!response.IsSuccessStatusCode)
                return (null, apiResponse?.Message ?? FallbackByStatus(response, "Đăng nhập thất bại"));

            return (apiResponse?.Data, apiResponse?.Data == null ? "Đăng nhập thất bại" : null);
        }

        // 2. Đăng ký
        public async Task<(bool success, string? error)> Register(RegisterRequestDto request)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, request, _jsonOptions);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return (false, "Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.");
            }

            var apiResponse = await TryReadEnvelopeAsync<bool>(response);

            if (!response.IsSuccessStatusCode || apiResponse == null || !apiResponse.Success)
                return (false, apiResponse?.Message ?? FallbackByStatus(response, "Đăng ký thất bại"));

            return (true, null);
        }

        /// <summary>Đọc vỏ ApiResponse&lt;T&gt; an toàn — body rỗng / không phải JSON (vd 429 không body) → null.</summary>
        private async Task<ApiResponse<T>?> TryReadEnvelopeAsync<T>(HttpResponseMessage response)
        {
            try
            {
                var raw = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return JsonSerializer.Deserialize<ApiResponse<T>>(raw, _jsonOptions);
            }
            catch (JsonException) { return null; }
        }

        private static string FallbackByStatus(HttpResponseMessage r, string generic) => (int)r.StatusCode switch
        {
            429 => "Bạn thao tác quá nhanh. Vui lòng chờ một lát rồi thử lại.",
            401 or 403 => generic,
            >= 500 => "Máy chủ đang gặp sự cố. Vui lòng thử lại sau.",
            _ => generic
        };

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

        // 6. Đăng xuất — thu hồi refresh token phía backend (best-effort).
        public async Task LogoutAsync(string? refreshToken)
        {
            try
            {
                await _httpClient.PostAsJsonAsync(
                    ApiRoutes.Auth.Logout, new { refreshToken }, _jsonOptions);
            }
            catch { /* best-effort — vẫn xoá phiên phía WebApp dù backend lỗi */ }
        }

        // 7. Quên mật khẩu — không lộ email có tồn tại hay không.
        public async Task<ApiResponse<bool>> ForgotPasswordAsync(string email)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    ApiRoutes.Auth.ForgotPassword, new { email }, _jsonOptions);
                var result = await TryReadEnvelopeAsync<bool>(response);
                if (!response.IsSuccessStatusCode)
                    return ApiResponse<bool>.ErrorResponse(
                        result?.Message ?? FallbackByStatus(response, "Không gửi được yêu cầu. Vui lòng thử lại."));
                return result ?? ApiResponse<bool>.SuccessResponse(true);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return ApiResponse<bool>.ErrorResponse("Không kết nối được máy chủ. Vui lòng thử lại.");
            }
        }

        // 8. Đặt lại mật khẩu bằng token trong email.
        public async Task<ApiResponse<bool>> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    ApiRoutes.Auth.ResetPassword, new { token, newPassword }, _jsonOptions);
                var result = await TryReadEnvelopeAsync<bool>(response);
                if (!response.IsSuccessStatusCode)
                    return ApiResponse<bool>.ErrorResponse(
                        result?.Message ?? FallbackByStatus(response, "Không đặt lại được mật khẩu. Liên kết có thể đã hết hạn."));
                return result ?? ApiResponse<bool>.ErrorResponse("Phản hồi từ máy chủ không hợp lệ");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return ApiResponse<bool>.ErrorResponse("Không kết nối được máy chủ. Vui lòng thử lại.");
            }
        }
    }
}