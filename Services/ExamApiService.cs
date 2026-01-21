using System.Text.Json;
using System.Text;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services
{
    public class ExamApiService
    {
        private readonly HttpClient _httpClient;
        // Đảm bảo Port 7290 khớp với Backend của bạn
        private readonly string _apiBaseUrl = "https://localhost:7290/api";
        private readonly JsonSerializerOptions _jsonOptions;

        public ExamApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        // 1. Lấy đề thi (Giữ nguyên)
        public async Task<ExerciseDetailDto?> GetExerciseById(int id)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/exercises/{id}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();

            // Thử giải nén theo cấu trúc bọc ApiResponse
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ExerciseDetailDto>>(json, _jsonOptions);

            if (apiResponse?.Data != null)
            {
                // Kiểm tra xem Questions có bị rỗng do lệch tên trường không
                if (apiResponse.Data.Questions == null || apiResponse.Data.Questions.Count == 0)
                {
                    // Nếu rỗng, thử giải nén lại với giả định tên trường là 'exerciseQuestions'
                    // Bạn có thể tạm thời sửa file ExerciseDetailDto.cs thuộc tính Questions 
                    // thành [JsonPropertyName("exerciseQuestions")] để test nhanh.
                }
                return apiResponse.Data;
            }

            return null;
        }

        // 2. Bắt đầu làm bài (Lấy AttemptId)
        public async Task<int> StartExercise(int exerciseId, int studentId)
        {
            var payload = new StartExerciseDto { ExerciseId = exerciseId, StudentId = studentId };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/ExerciseAttempts/start", content);
            if (!response.IsSuccessStatusCode) return 0;

            var resString = await response.Content.ReadAsStringAsync();
            var apiResult = JsonSerializer.Deserialize<ApiResponse<ExerciseAttemptResponseDto>>(resString, _jsonOptions);

            return apiResult?.Data?.AttemptId ?? 0;
        }

        // 3. Nộp từng câu trả lời
        public async Task<bool> SubmitSingleAnswer(SubmitAnswerRequestDto dto)
        {
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/ExerciseAttempts/submit-answer", content);
            return response.IsSuccessStatusCode;
        }

        // 4. Hoàn thành bài thi
        public async Task<bool> CompleteExercise(int attemptId)
        {
            var payload = new CompleteExerciseDto { AttemptId = attemptId };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/ExerciseAttempts/complete", content);
            return response.IsSuccessStatusCode;
        }
        public async Task<ExerciseResultDto?> GetExerciseResult(int attemptId)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/ExerciseAttempts/{attemptId}/result");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ExerciseResultDto>>(json, _jsonOptions);

            return apiResponse?.Data;
        }
    }
}