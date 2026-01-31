using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Dịch vụ kết nối và lấy dữ liệu khóa học, bài giảng từ Backend API
    /// </summary>
    public class CourseApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public CourseApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;

            // Cấu hình để không phân biệt chữ hoa chữ thường khi giải mã JSON từ API
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Tự động lấy JWT Token từ Session (nếu có) và gắn vào Header Authorization
        /// </summary>
        private void AddAuthHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        /// <summary>
        /// Lấy toàn bộ danh sách bài giảng (Dùng cho trang Index Lesson)
        /// URL: GET /api/Lesson
        /// </summary>
        public async Task<IEnumerable<LessonDto>> GetAllLessonsAsync()
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/Lesson");

                if (response.IsSuccessStatusCode)
                {
                    var resString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<IEnumerable<LessonDto>>>(resString, _jsonOptions);
                    return apiResponse?.Data ?? new List<LessonDto>();
                }
                return new List<LessonDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Lỗi GetAllLessons: {ex.Message} ---");
                return new List<LessonDto>();
            }
        }

        /// <summary>
        /// Lấy chi tiết nội dung một bài học bao gồm Video, văn bản, công thức...
        /// URL: GET /api/Lesson/{id}
        /// </summary>
        public async Task<LessonDto?> GetLessonDetailAsync(int lessonId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/Lesson/{lessonId}");

                if (response.IsSuccessStatusCode)
                {
                    var resString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<LessonDto>>(resString, _jsonOptions);
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Lỗi GetLessonDetail: {ex.Message} ---");
                return null;
            }
        }

        /// <summary>
        /// Lấy danh sách bài giảng thuộc cùng một chủ đề (Dùng cho Sidebar)
        /// URL: GET /api/Lesson/by-topic/{topicId}
        /// </summary>
        public async Task<IEnumerable<LessonDto>> GetLessonsByTopicAsync(int topicId)
        {
            try
            {
                AddAuthHeader();
                // Sử dụng route "by-topic" để khớp với cấu hình Backend
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/Lesson/by-topic/{topicId}");

                if (response.IsSuccessStatusCode)
                {
                    var resString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<IEnumerable<LessonDto>>>(resString, _jsonOptions);
                    return apiResponse?.Data ?? new List<LessonDto>();
                }
                return new List<LessonDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Lỗi GetLessonsByTopic: {ex.Message} ---");
                return new List<LessonDto>();
            }
        }

        /// <summary>
        /// Lấy toàn bộ cấu trúc phân cấp của chương trình học (Chương -> Chủ đề -> Bài học)
        /// URL: GET /api/Curriculum/{id}
        /// </summary>
        public async Task<CurriculumDto?> GetCurriculumDetailAsync(int curriculumId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/Curriculum/{curriculumId}");

                if (response.IsSuccessStatusCode)
                {
                    var resString = await response.Content.ReadAsStringAsync();
                    // THÊM DÒNG NÀY:
                    Console.WriteLine("DỮ LIỆU THÔ TỪ API: " + resString);

                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<CurriculumDto>>(resString, _jsonOptions);
                    return apiResponse?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Lỗi GetCurriculumDetail: {ex.Message} ---");
                return null;
            }
        }

        /// <summary>
        /// Lấy danh sách bài tập vận dụng cho một chủ đề cụ thể
        /// URL: GET /api/Exercise/by-topic/{topicId}
        /// </summary>
        public async Task<List<ExerciseDto>> GetExercisesByTopicAsync(int topicId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/Exercise/by-topic/{topicId}");

                if (response.IsSuccessStatusCode)
                {
                    var resString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ExerciseDto>>>(resString, _jsonOptions);
                    return apiResponse?.Data ?? new List<ExerciseDto>();
                }
                return new List<ExerciseDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Lỗi GetExercisesByTopic: {ex.Message} ---");
                return new List<ExerciseDto>();
            }
        }
    }
}