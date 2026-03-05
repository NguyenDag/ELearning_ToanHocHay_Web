using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services
{
    public class CourseApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public CourseApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        private void AddAuthHeader()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // --- CÁC HÀM GỌI API (Dùng đường dẫn ngắn, bỏ ApiConstant) ---

        public async Task<IEnumerable<LessonDto>> GetAllLessonsAsync()
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<LessonDto>>>("Lesson", _jsonOptions);
                return response?.Data ?? new List<LessonDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetAllLessons: {ex.Message}");
                return new List<LessonDto>();
            }
        }

        public async Task<LessonDto?> GetLessonDetailAsync(int lessonId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<LessonDto>>($"Lesson/{lessonId}", _jsonOptions);
                return response?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetLessonDetail: {ex.Message}");
                return null;
            }
        }

        public async Task<IEnumerable<LessonDto>> GetLessonsByTopicAsync(int topicId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<LessonDto>>>($"Lesson/by-topic/{topicId}", _jsonOptions);
                return response?.Data ?? new List<LessonDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetLessonsByTopic: {ex.Message}");
                return new List<LessonDto>();
            }
        }
        public async Task<CoreDashboardDto?> GetStudentDashboardStatsAsync(int studentId)
        {
            try
            {
                AddAuthHeader();
                // Gọi đến endpoint dashboard của Backend (Dự án Control)
                var response = await _httpClient.GetFromJsonAsync<CoreDashboardDto>($"student/{studentId}/dashboard/overview", _jsonOptions);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetStudentDashboardStats: {ex.Message}");
                return null;
            }
        }
        
        public async Task<ApiResponse<bool>> UpdateProfileAsync(UpdateProfileDto dto)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.PostAsJsonAsync("Student/update-profile", dto);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions);

                // Sửa dòng này:
                return result ?? new ApiResponse<bool> { Success = false, Message = "Không nhận được phản hồi" };
            }
            catch (Exception ex)
            {
                // Và sửa dòng này:
                return new ApiResponse<bool> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CurriculumDto?> GetCurriculumDetailAsync(int id)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<CurriculumDto>>($"Curriculum/{id}", _jsonOptions);
                return (response != null && response.Success) ? response.Data : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetCurriculum: {ex.Message}");
                return null;
            }
        }
        // Thêm vào CourseApiService.cs phía WebApp
        public async Task<List<CurriculumDto>> GetFullMenuTreeAsync()
        {
            try
            {
                AddAuthHeader();
                // Gọi tới endpoint mới tạo ở Bước 1
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CurriculumDto>>>("Curriculum/full-tree", _jsonOptions);
                return response?.Data ?? new List<CurriculumDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error get menu tree: {ex.Message}");
                return new List<CurriculumDto>();
            }
        }

        public async Task<List<ExerciseDto>> GetExercisesByTopicAsync(int topicId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ExerciseDto>>>($"Exercise/by-topic/{topicId}", _jsonOptions);
                return response?.Data ?? new List<ExerciseDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetExercisesByTopic: {ex.Message}");
                return new List<ExerciseDto>();
            }
        }
    }
}