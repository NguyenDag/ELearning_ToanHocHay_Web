using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services.Admin
{
    /// <summary>Danh mục + khoá học + phiên bản + cây nội dung + block (quản trị).</summary>
    public class ContentAdminApiService
    {
        private readonly ApiClient _api;

        public ContentAdminApiService(ApiClient api) => _api = api;

        // ---------------- catalog ----------------
        public async Task<CatalogPageVm> GetCatalogAsync()
        {
            var subjects = await _api.GetAsync<List<SubjectRow>>($"{ApiRoutes.CatalogAuthoring.Subjects}?includeInactive=true");
            var grades = await _api.GetAsync<List<GradeRow>>($"{ApiRoutes.CatalogAuthoring.GradeLevels}?includeInactive=true");
            var frameworks = await _api.GetAsync<List<FrameworkRow>>($"{ApiRoutes.CatalogAuthoring.Frameworks}?includeInactive=true");
            return new CatalogPageVm
            {
                Subjects = (subjects.Data ?? new()).Select(x => x.ToItem()).ToList(),
                Grades = (grades.Data ?? new()).Select(x => x.ToItem()).ToList(),
                Frameworks = (frameworks.Data ?? new()).Select(x => x.ToItem()).ToList()
            };
        }

        public Task<ApiResult> SaveSubjectAsync(int? id, object body)
            => id is > 0 ? _api.PutAsync(ApiRoutes.CatalogAuthoring.Subject(id.Value), body)
                         : _api.PostAsync(ApiRoutes.CatalogAuthoring.Subjects, body);

        public Task<ApiResult> SaveGradeAsync(int? id, object body)
            => id is > 0 ? _api.PutAsync(ApiRoutes.CatalogAuthoring.GradeLevel(id.Value), body)
                         : _api.PostAsync(ApiRoutes.CatalogAuthoring.GradeLevels, body);

        public Task<ApiResult> SaveFrameworkAsync(int? id, object body)
            => id is > 0 ? _api.PutAsync(ApiRoutes.CatalogAuthoring.Framework(id.Value), body)
                         : _api.PostAsync(ApiRoutes.CatalogAuthoring.Frameworks, body);

        // ---------------- courses ----------------
        public async Task<List<CourseAdminDto>> ListCoursesAsync(int? subjectId, int? gradeLevelId)
        {
            var qs = new List<string> { "includeUnpublished=true" };
            if (subjectId is > 0) qs.Add($"subjectId={subjectId}");
            if (gradeLevelId is > 0) qs.Add($"gradeLevelId={gradeLevelId}");
            var r = await _api.GetAsync<List<CourseAdminDto>>($"{ApiRoutes.Courses.List}?{string.Join('&', qs)}");
            return r.IsSuccess && r.Data != null ? r.Data : new();
        }

        public async Task<CourseAdminDto?> GetCourseAsync(int id)
        {
            var r = await _api.GetAsync<CourseAdminDto>(ApiRoutes.Courses.ById(id));
            return r.IsSuccess ? r.Data : null;
        }

        public Task<ApiResult<CourseAdminDto>> CreateCourseAsync(CourseEditVm vm)
            => _api.PostAsync<CourseAdminDto>(ApiRoutes.Courses.List, CoursePayload(vm));

        public Task<ApiResult<CourseAdminDto>> UpdateCourseAsync(int id, CourseEditVm vm)
            => _api.PutAsync<CourseAdminDto>(ApiRoutes.Courses.ById(id), CoursePayload(vm));

        public Task<ApiResult> ArchiveCourseAsync(int id) => _api.PostAsync(ApiRoutes.Courses.Archive(id));
        public Task<ApiResult> UnarchiveCourseAsync(int id) => _api.PostAsync(ApiRoutes.Courses.Unarchive(id));

        // ---------------- versions ----------------
        public async Task<List<CourseVersionDto>> ListVersionsAsync(int courseId)
        {
            var r = await _api.GetAsync<List<CourseVersionDto>>(ApiRoutes.Courses.Versions(courseId));
            return r.IsSuccess && r.Data != null ? r.Data : new();
        }

        public Task<ApiResult<CourseVersionDto>> CreateVersionAsync(int courseId, string? label, int? cloneFromVersionId)
            => _api.PostAsync<CourseVersionDto>(ApiRoutes.Courses.Versions(courseId),
                new { Label = label, CloneFromVersionId = cloneFromVersionId });

        public Task<ApiResult> SubmitVersionAsync(int versionId) => _api.PostAsync(ApiRoutes.Courses.SubmitVersion(versionId));

        public Task<ApiResult> ReviewVersionAsync(int versionId, ReviewDecision decision, string? summary)
            => _api.PostAsync(ApiRoutes.Courses.ReviewVersion(versionId),
                new { Decision = decision, Summary = summary, Comments = Array.Empty<object>() });

        public Task<ApiResult> PublishVersionAsync(int versionId) => _api.PostAsync(ApiRoutes.Courses.PublishVersion(versionId));
        public Task<ApiResult> ArchiveVersionAsync(int versionId) => _api.PostAsync(ApiRoutes.Courses.ArchiveVersion(versionId));

        public async Task<List<ContentReviewDto>> GetVersionReviewsAsync(int versionId)
        {
            var r = await _api.GetAsync<List<ContentReviewDto>>(ApiRoutes.Courses.VersionReviews(versionId));
            return r.IsSuccess && r.Data != null ? r.Data : new();
        }

        public Task<ApiResult> ResolveCommentAsync(int commentId) => _api.PostAsync(ApiRoutes.Courses.ResolveComment(commentId));

        // ---------------- content tree ----------------
        public async Task<List<ContentNodeDto>> GetTreeAsync(int versionId)
        {
            var r = await _api.GetAsync<List<ContentNodeDto>>(ApiRoutes.ContentAuthoring.Tree(versionId));
            return r.IsSuccess && r.Data != null ? r.Data : new();
        }

        public async Task<ContentNodeDetailDto?> GetNodeAsync(int nodeId)
        {
            var r = await _api.GetAsync<ContentNodeDetailDto>(ApiRoutes.ContentAuthoring.Node(nodeId));
            return r.IsSuccess ? r.Data : null;
        }

        public Task<ApiResult> CreateNodeAsync(int versionId, NodeType type, string title, int? parentNodeId, bool isFree, int? durationMinutes)
            => _api.PostAsync(ApiRoutes.ContentAuthoring.CreateNode(versionId),
                new { ParentNodeId = parentNodeId, NodeType = type, Title = title, IsFree = isFree, DurationMinutes = durationMinutes });

        public Task<ApiResult> UpdateNodeAsync(int nodeId, string? title, string? slug, bool? isFree, bool? isHidden, int? durationMinutes)
            => _api.PutAsync(ApiRoutes.ContentAuthoring.Node(nodeId),
                new { Title = title, Slug = slug, IsFree = isFree, IsHidden = isHidden, DurationMinutes = durationMinutes });

        public Task<ApiResult> DeleteNodeAsync(int nodeId) => _api.DeleteAsync(ApiRoutes.ContentAuthoring.Node(nodeId));

        public async Task<ApiResult> MoveNodeAsync(int nodeId, int? newParentNodeId, int? orderIndex)
        {
            var r = await _api.PatchAsync<object>(ApiRoutes.ContentAuthoring.MoveNode(nodeId),
                new { NewParentNodeId = newParentNodeId, OrderIndex = orderIndex });
            return ApiResult.From(r);
        }

        public Task<ApiResult> ReorderAsync(int versionId, int? parentNodeId, List<int> orderedNodeIds)
            => _api.PostAsync(ApiRoutes.ContentAuthoring.Reorder(versionId, parentNodeId),
                new { OrderedNodeIds = orderedNodeIds });

        // ---------------- blocks ----------------
        public Task<ApiResult> AddBlockAsync(int nodeId, LessonBlockType type, string? text, string? url, string? metadataJson, int? orderIndex)
            => _api.PostAsync(ApiRoutes.ContentAuthoring.Blocks(nodeId),
                new { BlockType = type, ContentText = text, ContentUrl = url, MetadataJson = metadataJson, OrderIndex = orderIndex });

        public Task<ApiResult> UpdateBlockAsync(int blockId, LessonBlockType type, string? text, string? url, string? metadataJson, int? orderIndex)
            => _api.PutAsync(ApiRoutes.ContentAuthoring.Block(blockId),
                new { BlockType = type, ContentText = text, ContentUrl = url, MetadataJson = metadataJson, OrderIndex = orderIndex });

        public Task<ApiResult> DeleteBlockAsync(int blockId) => _api.DeleteAsync(ApiRoutes.ContentAuthoring.Block(blockId));

        // ---------------- helpers ----------------
        private static object CoursePayload(CourseEditVm vm) => new
        {
            vm.SubjectId,
            vm.GradeLevelId,
            vm.FrameworkId,
            vm.Title,
            vm.Slug,
            vm.Description,
            vm.ThumbnailUrl,
            vm.ListPrice,
            vm.SalePrice,
            vm.IsPurchasable,
            vm.AccessDurationDays,
            vm.DisplayOrder
        };

        private sealed class SubjectRow
        {
            public int SubjectId { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string? Slug { get; set; }
            public int DisplayOrder { get; set; }
            public bool IsActive { get; set; }
            public CatalogItemDto ToItem() => new()
            { Id = SubjectId, Code = Code, Name = Name, Slug = Slug, DisplayOrder = DisplayOrder, IsActive = IsActive };
        }

        private sealed class GradeRow
        {
            public int GradeLevelId { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public EducationStage Stage { get; set; }
            public int DisplayOrder { get; set; }
            public bool IsActive { get; set; }
            public CatalogItemDto ToItem() => new()
            { Id = GradeLevelId, Code = Code, Name = Name, Stage = Stage, DisplayOrder = DisplayOrder, IsActive = IsActive };
        }

        private sealed class FrameworkRow
        {
            public int FrameworkId { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string? Publisher { get; set; }
            public bool IsActive { get; set; }
            public CatalogItemDto ToItem() => new()
            { Id = FrameworkId, Code = Code, Name = Name, Publisher = Publisher, IsActive = IsActive };
        }
    }
}
