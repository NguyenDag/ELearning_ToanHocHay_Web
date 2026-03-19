using System.Net.Http.Json;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace ToanHocHay.WebApp.Services
{
    public class ExamApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public ExamApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            // Cấu hình để không phân biệt chữ hoa chữ thường khi giải mã JSON từ API
            // VÀ giữ nguyên PascalCase khi gửi đi để khớp với Backend (PropertyNamingPolicy = null)
            _jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null 
            };
        }

        /// <summary>
        /// Tự động lấy JWT Token từ Session và gắn vào Header Authorization
        /// </summary>
        private void AddAuthHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
             if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                Console.WriteLine("--- CẢNH BÁO: Không tìm thấy Token trong Session! ---");
            }
        }

        // 1. Lấy danh sách bài kiểm tra (Trang Index)
        public async Task<List<ExerciseDto>> GetExercisesAsync()
        {
            try
            {
                AddAuthHeader(); // Gắn Token vào yêu cầu
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/Exercises");

                var resString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Log để kiểm tra dữ liệu thô từ API
                    Console.WriteLine($"--- DỮ LIỆU THÔ TỪ API: {resString} ---");

                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ExerciseDto>>>(resString, _jsonOptions);
                    return apiResponse?.Data ?? new List<ExerciseDto>();
                }

                // In ra màn hình console của Visual Studio mã lỗi chi tiết (401, 404, 500...)
                Console.WriteLine($"--- LỖI API EXERCISES: {(int)response.StatusCode} - {resString} ---");
                return new List<ExerciseDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- LỖI KẾT NỐI MẠNG: {ex.Message} ---");
                return new List<ExerciseDto>();
            }
        }

        // 2. Lấy chi tiết đề thi kèm câu hỏi
        public async Task<ExerciseDetailDto?> GetExerciseById(int id)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/exercises/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var resString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"=== EXERCISE DETAIL JSON: {resString} ===");
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<ExerciseDetailDto>>(resString, _jsonOptions);
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Lỗi lấy chi tiết đề thi: {ex.Message} ---");
                return null;
            }
        }

        // 3. Bắt đầu làm bài (Tạo AttemptId)
        public async Task<(int attemptId, string? error)> StartExercise(int exerciseId, int studentId)
        {
            try
            {
                AddAuthHeader();
                var payload = new { ExerciseId = exerciseId, StudentId = studentId };

                // Gọi API
                var response = await _httpClient.PostAsJsonAsync($"{ApiConstant.apiBaseUrl}/api/ExerciseAttempts/start", payload, _jsonOptions);
                var resString = await response.Content.ReadAsStringAsync();

                /*if (!response.IsSuccessStatusCode && response.Message)
                {
                    // In ra log để xem Backend thực sự báo lỗi gì (Ví dụ: 401 Unauthorized)
                    Console.WriteLine($"--- LỖI BACKEND: {resString}");
                    return (0, "Máy chủ từ chối yêu cầu (có thể do hết hạn phiên làm việc).");
                }*/

                // Dùng dynamic hoặc kiểm tra kỹ Data null
                var apiResult = JsonSerializer.Deserialize<ApiResponse<ExerciseAttemptDto>>(resString, _jsonOptions);

                if (apiResult != null && apiResult.Success && apiResult.Data != null)
                {
                    return (apiResult.Data.AttemptId, null);
                }

                return (0, apiResult?.Message ?? "Không thể khởi tạo bài thi.");
            }
            catch (Exception ex)
            {
                // Log lỗi thật sự ra Console để debug
                Console.WriteLine($"--- DEBUG START EXERCISE ERROR: {ex.ToString()}");
                return (0, "Lỗi kết nối hoặc dữ liệu không hợp lệ.");
            }
        }

        // 4. Nộp từng câu trả lời (Ajax/Realtime)
        /*public async Task<bool> SubmitSingleAnswer(SubmitAnswerRequestDto dto)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.PostAsJsonAsync($"{ApiConstant.apiBaseUrl}/api/ExerciseAttempts/submit-answer", dto);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }*/

        // 4. Nộp từng câu trả lời (Ajax/Realtime) (Updated for SubmitSingleAnswer)
        public async Task<bool> SaveSingleAnswer(SubmitAnswerRequestDto dto)
        {
            try
            {
                AddAuthHeader();
                Console.WriteLine($"=== SAVE ANSWER: qId={dto.QuestionId}, optId={dto.SelectedOptionId}, text={dto.AnswerText} ===");

                var response = await _httpClient.PostAsJsonAsync(
                    $"{ApiConstant.apiBaseUrl}/api/ExerciseAttempts/save-answer", dto);

                // THÊM ĐOẠN NÀY
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"=== SAVE FAILED {(int)response.StatusCode}: {err} ===");
                }

                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // 5. Hoàn thành và tính điểm bài thi
        public async Task<bool> CompleteExercise(int attemptId)
        {
            try
            {
                AddAuthHeader();
                var payload = new { AttemptId = attemptId };
                var response = await _httpClient.PostAsJsonAsync($"{ApiConstant.apiBaseUrl}/api/ExerciseAttempts/complete", payload);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // 6. Lấy kết quả báo cáo sau khi thi xong
        public async Task<ExerciseResultDto?> GetExerciseResult(int attemptId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/ExerciseAttempts/{attemptId}/result");

                if (response.IsSuccessStatusCode)
                {
                    var resString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<ExerciseResultDto>>(resString, _jsonOptions);
                    return apiResponse?.Data;
                }
                return null;
            }
            catch { return null; }
        }

        // 7. Gọi AI Gợi ý
        public async Task<AIHintDto?> GetAIHintAsync(AIHintRequestDto dto)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.PostAsJsonAsync($"{ApiConstant.apiBaseUrl}/api/AIHint", dto);

                if (response.IsSuccessStatusCode)
                {
                    var resString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<AIHintDto>>(resString, _jsonOptions);
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Lỗi GetAIHint: {ex.Message} ---");
                return null;
            }
        }

        // 8. Báo cáo chuyển tab — gửi email cho phụ huynh
        public async Task<bool> ReportTabSwitchAsync(int attemptId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.PostAsync(
                    $"{ApiConstant.apiBaseUrl}/api/ExerciseAttempts/{attemptId}/report-tab-switch",
                    null
                );
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Lỗi ReportTabSwitch: {ex.Message} ---");
                return false;
            }
        }
        // 9. Lấy lịch sử chuyển tab
        public async Task<List<DateTime>> GetTabSwitchLogsAsync(int attemptId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/ExerciseAttempts/{attemptId}/tab-switch-logs");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DateTime>>>();
                    return result?.Data ?? new List<DateTime>();
                }
                return new List<DateTime>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Lỗi GetTabSwitchLogs: {ex.Message} ---");
                return new List<DateTime>();
            }
        }
    }
}
