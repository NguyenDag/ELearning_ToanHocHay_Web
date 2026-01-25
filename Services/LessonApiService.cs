using System.Net.Http.Json;

using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Services
{
    public class LessonApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://localhost:7092/api/Lesson"; // Thay bằng Port API của bạn

        public LessonApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Lấy chi tiết bài học
        public async Task<LessonDto?> GetByIdAsync(int lessonId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<LessonDto>>($"{_baseUrl}/{lessonId}");
                return response?.Data;
            }
            catch { return null; }
        }

        // Lấy danh sách bài học theo Topic
        public async Task<IEnumerable<LessonDto>> GetByTopicAsync(int topicId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<LessonDto>>>($"{_baseUrl}/by-topic/{topicId}");
                return response?.Data ?? new List<LessonDto>();
            }
            catch { return new List<LessonDto>(); }
        }
    }
}