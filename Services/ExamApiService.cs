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
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
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
        public async Task<int> StartExercise(int exerciseId, int studentId)
        {
            try
            {
                AddAuthHeader();
                var payload = new { ExerciseId = exerciseId, StudentId = studentId };
                var response = await _httpClient.PostAsJsonAsync($"{ApiConstant.apiBaseUrl}/api/ExerciseAttempts/start", payload);

                var resString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // In log lỗi chi tiết từ Backend để debug nhanh
                    Console.WriteLine($"--- LỖI API START: {resString} ---");
                    return 0;
                }

                // Giải mã JSON dựa trên class ExerciseAttemptDto vừa tạo
                var apiResult = JsonSerializer.Deserialize<ApiResponse<ExerciseAttemptDto>>(resString, _jsonOptions);
                return apiResult?.Data?.AttemptId ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- LỖI KẾT NỐI START EXERCISE: {ex.Message} ---");
                return 0;
            }
        }

        // 4. Nộp từng câu trả lời (Ajax/Realtime)
        public async Task<bool> SubmitSingleAnswer(SubmitAnswerRequestDto dto)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.PostAsJsonAsync($"{ApiConstant.apiBaseUrl}/api/ExerciseAttempts/submit-answer", dto);
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
    }
}