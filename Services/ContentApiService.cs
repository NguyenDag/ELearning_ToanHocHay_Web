using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs.Content;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Tầng nội dung học mới (P2): catalog · courses · learn · enrollments · progress.
    /// Đi qua <see cref="ApiClient"/> nên giữ mã HTTP thật (403 nội dung khoá, 429 khách vượt hạn mức).
    /// Các endpoint learn/catalog/courses cho phép ẩn danh — khách xem được node miễn phí.
    /// </summary>
    public class ContentApiService
    {
        private readonly ApiClient _api;

        public ContentApiService(ApiClient api) => _api = api;

        // ---- catalog ----
        public Task<ApiResult<List<SubjectDto>>> GetSubjectsAsync()
            => _api.GetAsync<List<SubjectDto>>(ApiRoutes.Catalog.Subjects);

        public Task<ApiResult<List<GradeLevelDto>>> GetGradeLevelsAsync()
            => _api.GetAsync<List<GradeLevelDto>>(ApiRoutes.Catalog.GradeLevels);

        // ---- courses ----
        public Task<ApiResult<List<CourseSummaryDto>>> GetCoursesAsync(
            int? subjectId = null, int? gradeLevelId = null, bool publishedOnly = true)
        {
            var q = new List<string> { $"publishedOnly={publishedOnly.ToString().ToLowerInvariant()}" };
            if (subjectId is > 0) q.Add($"subjectId={subjectId}");
            if (gradeLevelId is > 0) q.Add($"gradeLevelId={gradeLevelId}");
            return _api.GetAsync<List<CourseSummaryDto>>($"{ApiRoutes.Courses.List}?{string.Join("&", q)}");
        }

        public Task<ApiResult<CourseSummaryDto>> GetCourseAsync(int courseId)
            => _api.GetAsync<CourseSummaryDto>(ApiRoutes.Courses.ById(courseId));

        // ---- learn ----
        public Task<ApiResult<CourseContentDto>> GetCourseContentAsync(int courseId)
            => _api.GetAsync<CourseContentDto>(ApiRoutes.Learn.CourseContent(courseId));

        public Task<ApiResult<ContentNodeDetailDto>> GetNodeAsync(int nodeId)
            => _api.GetAsync<ContentNodeDetailDto>(ApiRoutes.Learn.Node(nodeId));

        // ---- enrollments ----
        public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync()
            => _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.Enrollments.Mine);

        public Task<ApiResult<EnrolmentDto>> EnrollAsync(int courseId)
            => _api.PostAsync<EnrolmentDto>(ApiRoutes.Enrollments.EnrollCourse(courseId));

        // ---- progress ----
        public Task<ApiResult<NodeProgressDto>> MarkLessonCompleteAsync(int nodeId, int secondsViewed)
            => _api.PostAsync<NodeProgressDto>(ApiRoutes.Progress.CompleteLesson(nodeId), new { secondsViewed });

        public Task<ApiResult<List<NodeProgressDto>>> GetVersionProgressAsync(int courseVersionId)
            => _api.GetAsync<List<NodeProgressDto>>(ApiRoutes.Progress.Version(courseVersionId));
    }
}
