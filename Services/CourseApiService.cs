using System.Net.Http.Json;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Nội dung khoá học. Token do <see cref="AuthTokenHandler"/> tự gắn.
    ///
    /// ⚠️ Nhiều hàm dưới đây gọi các endpoint cũ (<c>Lesson</c>, <c>Curriculum</c>,
    /// <c>LessonProgress</c>, <c>Exercise/by-topic</c>, <c>Student/update-profile</c>) mà backend
    /// đã BỎ khi làm tầng nội dung mới. Việc thay bằng <c>api/learn</c> / <c>api/catalog</c> /
    /// <c>api/courses</c> / <c>api/enrollments</c> / <c>api/progress</c> nằm ở Đợt 2 của kế hoạch.
    /// Tạm thời các hàm này trả rỗng/null (đã bọc try/catch) để app không vỡ.
    /// </summary>
    public class CourseApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = ApiJson.Options;

        public CourseApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // TODO(Đợt 2): thay bằng GET /api/progress/versions/{courseVersionId}
        public async Task<List<int>> GetCompletedLessonIdsAsync(int studentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"progress/students/{studentId}/completed-lessons");
                if (!response.IsSuccessStatusCode) return new List<int>();
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<List<int>>>(json, _jsonOptions);
                return result?.Data ?? new List<int>();
            }
            catch { return new List<int>(); }
        }

        // TODO(Đợt 2): GET /api/learn/courses/{courseId}/content
        public async Task<IEnumerable<LessonDto>> GetAllLessonsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<LessonDto>>>("learn/lessons", _jsonOptions);
                return response?.Data ?? new List<LessonDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetAllLessons: {ex.Message}");
                return new List<LessonDto>();
            }
        }

        // TODO(Đợt 2): GET /api/learn/nodes/{nodeId}
        public async Task<LessonDto?> GetLessonDetailAsync(int lessonId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<LessonDto>>(ApiRoutes.Learn.Node(lessonId), _jsonOptions);
                return response?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetLessonDetail: {ex.Message}");
                return null;
            }
        }

        // TODO(Đợt 2): nhánh con của cây learn
        public async Task<IEnumerable<LessonDto>> GetLessonsByTopicAsync(int topicId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<LessonDto>>>($"learn/nodes/{topicId}/children", _jsonOptions);
                return response?.Data ?? new List<LessonDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetLessonsByTopic: {ex.Message}");
                return new List<LessonDto>();
            }
        }

        // Route đổi student → students; backend nay bọc ApiResponse<T>.
        public async Task<CoreDashboardDto?> GetStudentDashboardStatsAsync(int studentId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<CoreDashboardDto>>(
                    ApiRoutes.Students.DashboardOverview(studentId), _jsonOptions);
                return response?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetStudentDashboardStats: {ex.Message}");
                return null;
            }
        }

        // TODO(Đợt 2): dựng cây từ catalog + courses + learn
        public async Task<CurriculumDto?> GetCurriculumDetailAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<CurriculumDto>>(ApiRoutes.Courses.ById(id), _jsonOptions);
                return (response != null && response.Success) ? response.Data : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi GetCurriculum: {ex.Message}");
                return null;
            }
        }

        // TODO(Đợt 2): GET /api/courses + /api/catalog
        public async Task<List<CurriculumDto>> GetFullMenuTreeAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CurriculumDto>>>(ApiRoutes.Courses.List, _jsonOptions);
                return response?.Data ?? new List<CurriculumDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error get menu tree: {ex.Message}");
                return new List<CurriculumDto>();
            }
        }

        // TODO(Đợt 2/3): danh sách đề theo node
        public async Task<List<ExerciseDto>> GetExercisesByTopicAsync(int topicId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ExerciseDto>>>(
                    $"{ApiRoutes.Exercises.List}?nodeId={topicId}", _jsonOptions);
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
