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
        private readonly ApiClient _api;
        private readonly JsonSerializerOptions _jsonOptions = ApiJson.Options;

        public ExamApiService(HttpClient httpClient, ApiClient api)
        {
            _httpClient = httpClient;
            _api = api;
        }

        /// <summary>Trạng thái sinh nhận xét AI nền cho lượt làm bài (poll ở trang kết quả).</summary>
        public Task<ApiResult<FeedbackStatusDto>> GetFeedbackStatusAsync(int attemptId)
            => _api.GetAsync<FeedbackStatusDto>(ApiRoutes.ExerciseAttempts.FeedbackStatus(attemptId));

        /// <summary>Hạn mức gợi ý AI còn lại trong ngày.</summary>
        public Task<ApiResult<AiHintQuotaDto>> GetHintQuotaAsync()
            => _api.GetAsync<AiHintQuotaDto>(ApiRoutes.AiHints.Quota);

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
        // Backend lấy StudentId TỪ TOKEN. 403 khi tier gói < Exercise.RequiredTier;
        // 409/400 khi hết lượt (MaxAttempts) hoặc đề chưa publish.
        public async Task<(int attemptId, bool needUpgrade, string? error)> StartExercise(int exerciseId)
        {
            var r = await _api.PostAsync<ExerciseAttemptDto>(
                ApiRoutes.ExerciseAttempts.Start, new { ExerciseId = exerciseId });

            if (r.IsSuccess && r.Data is { AttemptId: > 0 })
                return (r.Data.AttemptId, false, null);

            return (0, r.IsForbidden, r.DisplayMessage);
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

        // 7. Gọi AI Gợi ý — api/ai-hints; 429 khi hết hạn mức ngày.
        public async Task<(AIHintDto? hint, bool quotaExceeded, string? error)> GetAIHintAsync(AIHintRequestDto dto)
        {
            var r = await _api.PostAsync<AIHintDto>(ApiRoutes.AiHints.Create, dto);
            if (r.IsSuccess && r.Data != null) return (r.Data, false, null);
            return (null, r.IsTooManyRequests, r.DisplayMessage);
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
