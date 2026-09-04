using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Dashboard học sinh (P4). Mọi endpoint nay bọc vỏ <c>ApiResponse&lt;T&gt;</c> và có
    /// tier-gating: <c>chapter-score-comparison</c> cần Standard+, <c>ai-assessment</c> /
    /// <c>ai-roadmap</c> cần Premium+ → trả <b>403</b> khi không đủ (giữ trong <see cref="ApiResult{T}"/>).
    /// </summary>
    public interface IDashboardApiService
    {
        Task<ApiResult<CoreDashboardDto>> GetStudentDashboardAsync(int studentId);
        Task<ApiResult<List<ChapterScoreDto>>> GetChapterScoreComparisonAsync(int studentId);
        Task<ApiResult<AIInsightResponse>> GetAIAssessmentAsync(int studentId);
        Task<ApiResult<AIInsightResponse>> GetAIRoadmapAsync(int studentId);
    }

    public class DashboardApiService : IDashboardApiService
    {
        private readonly ApiClient _api;

        public DashboardApiService(ApiClient api) => _api = api;

        public Task<ApiResult<CoreDashboardDto>> GetStudentDashboardAsync(int studentId)
            => _api.GetAsync<CoreDashboardDto>(ApiRoutes.Students.DashboardOverview(studentId));

        public Task<ApiResult<List<ChapterScoreDto>>> GetChapterScoreComparisonAsync(int studentId)
            => _api.GetAsync<List<ChapterScoreDto>>(ApiRoutes.Students.ChapterScoreComparison(studentId));

        public Task<ApiResult<AIInsightResponse>> GetAIAssessmentAsync(int studentId)
            => _api.GetAsync<AIInsightResponse>(ApiRoutes.Students.AiAssessment(studentId));

        public Task<ApiResult<AIInsightResponse>> GetAIRoadmapAsync(int studentId)
            => _api.GetAsync<AIInsightResponse>(ApiRoutes.Students.AiRoadmap(studentId));
    }

    public class ChapterScoreDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = "";
        public decimal AverageScore { get; set; }
    }
}
