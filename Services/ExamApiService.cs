using System.Net.Http.Json;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Luồng làm bài / thi. Route đã đổi sang <c>api/exercise-attempts</c>, <c>api/exercises</c>,
    /// <c>api/ai-hints</c> (A5). Token được <see cref="AuthTokenHandler"/> tự gắn + tự refresh.
    /// </summary>
    public class ExamApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = ApiJson.Options;

        public ExamApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<int>> GetCompletedExerciseIdsAsync(int studentId)
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiRoutes.ExerciseAttempts.History(studentId));
                if (!response.IsSuccessStatusCode) return new List<int>();
                var resString = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ExerciseResultDto>>>(resString, _jsonOptions);
                return apiResponse?.Data?
                    .Select(a => a.ExerciseId)
                    .Distinct()
                    .ToList() ?? new List<int>();
            }
            catch { return new List<int>(); }
        }

        // 1. Lấy danh sách bài kiểm tra (Trang Index)
        public async Task<List<ExerciseDto>> GetExercisesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiRoutes.Exercises.List);
                var resString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ExerciseDto>>>(resString, _jsonOptions);
                    return apiResponse?.Data ?? new List<ExerciseDto>();
                }

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
                var response = await _httpClient.GetAsync(ApiRoutes.Exercises.ById(id));
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

        // 3. Bắt đầu làm bài (Tạo AttemptId).
        // Backend nay lấy StudentId TỪ TOKEN (client gửi cũng bị ghi đè); 403 nếu tier gói < RequiredTier.
        public async Task<(int attemptId, string? error)> StartExercise(int exerciseId, int studentId)
        {
            try
            {
                var payload = new { ExerciseId = exerciseId, StudentId = studentId };
                var response = await _httpClient.PostAsJsonAsync(ApiRoutes.ExerciseAttempts.Start, payload, _jsonOptions);
                var resString = await response.Content.ReadAsStringAsync();

                var apiResult = JsonSerializer.Deserialize<ApiResponse<ExerciseAttemptDto>>(resString, _jsonOptions);

                if (response.IsSuccessStatusCode && apiResult is { Success: true, Data: not null })
                    return (apiResult.Data.AttemptId, null);

                return (0, apiResult?.Message ?? "Không thể khởi tạo bài thi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- DEBUG START EXERCISE ERROR: {ex}");
                return (0, "Lỗi kết nối hoặc dữ liệu không hợp lệ.");
            }
        }

        // 4. Nộp từng câu trả lời (Ajax/Realtime)
        public async Task<bool> SaveSingleAnswer(SubmitAnswerRequestDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiRoutes.ExerciseAttempts.SaveAnswer, dto, _jsonOptions);
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
                var payload = new { AttemptId = attemptId };
                var response = await _httpClient.PostAsJsonAsync(ApiRoutes.ExerciseAttempts.Complete, payload, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // 6. Lấy kết quả báo cáo sau khi thi xong
        public async Task<ExerciseResultDto?> GetExerciseResult(int attemptId)
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiRoutes.ExerciseAttempts.Result(attemptId));
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

        // 7. Gọi AI Gợi ý — route đổi thành api/ai-hints; 429 khi hết lượt hôm nay (xử lý ở Đợt 3).
        public async Task<AIHintDto?> GetAIHintAsync(AIHintRequestDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiRoutes.AiHints.Create, dto, _jsonOptions);
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
                var response = await _httpClient.PostAsync(ApiRoutes.ExerciseAttempts.ReportTabSwitch(attemptId), null);
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
                var response = await _httpClient.GetAsync(ApiRoutes.ExerciseAttempts.TabSwitchLogs(attemptId));
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DateTime>>>(_jsonOptions);
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
