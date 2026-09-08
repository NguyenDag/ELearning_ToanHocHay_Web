using System.ComponentModel.DataAnnotations;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Areas.Admin.Models
{
    public enum CourseStatus { Draft, Published, Archived }
    public enum VersionState { Draft, InReview, Approved, Published, Archived, Cloning }
    public enum NodeType { Chapter, Topic, SubTopic, Lesson }
    public enum ResourceType { Pdf, Slide, Doc, Sheet, ExternalLink }
    public enum ReviewDecision { Approve, RequestChanges, Reject }
    public enum CommentStatus { Open, Resolved }
    public enum EducationStage { Primary, LowerSecondary, UpperSecondary, ExamPrep, Other }

    public static class ContentEnums
    {
        public static string CourseStatusLabel(CourseStatus s) => s switch
        {
            CourseStatus.Draft => "Nháp", CourseStatus.Published => "Đã xuất bản", CourseStatus.Archived => "Lưu trữ", _ => s.ToString()
        };
        public static string VersionStateLabel(VersionState s) => s switch
        {
            VersionState.Draft => "Nháp",
            VersionState.InReview => "Đang duyệt",
            VersionState.Approved => "Đã duyệt",
            VersionState.Published => "Đã xuất bản",
            VersionState.Archived => "Lưu trữ",
            VersionState.Cloning => "Đang sao chép",
            _ => s.ToString()
        };
        public static string NodeTypeLabel(NodeType t) => t switch
        {
            NodeType.Chapter => "Chương", NodeType.Topic => "Chủ đề", NodeType.SubTopic => "Mục", NodeType.Lesson => "Bài học", _ => t.ToString()
        };
        public static string BlockTypeLabel(LessonBlockType t) => t switch
        {
            LessonBlockType.Heading => "Tiêu đề",
            LessonBlockType.Text => "Văn bản",
            LessonBlockType.Definition => "Định nghĩa",
            LessonBlockType.Example => "Ví dụ",
            LessonBlockType.Note => "Ghi chú",
            LessonBlockType.Formula => "Công thức",
            LessonBlockType.Image => "Hình ảnh",
            LessonBlockType.Video => "Video",
            LessonBlockType.Animation => "Hoạt hình",
            LessonBlockType.Embed => "Nhúng",
            LessonBlockType.Audio => "Âm thanh",
            LessonBlockType.Pdf => "PDF",
            _ => t.ToString()
        };
    }

    // ---------- catalog ----------
    public class CatalogItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Slug { get; set; }
        public string? Publisher { get; set; }
        public EducationStage Stage { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    // ---------- courses ----------
    public class CourseAdminDto
    {
        public int CourseId { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int GradeLevelId { get; set; }
        public string? GradeLevelName { get; set; }
        public int? FrameworkId { get; set; }
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public CourseStatus Status { get; set; }
        public decimal ListPrice { get; set; }
        public decimal? SalePrice { get; set; }
        public bool IsPurchasable { get; set; }
        public int? AccessDurationDays { get; set; }
        public int DisplayOrder { get; set; }
        public int? PublishedVersionId { get; set; }
        public int? PublishedVersionNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CourseEditVm
    {
        public int CourseId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Chọn môn")] public int SubjectId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Chọn lớp")] public int GradeLevelId { get; set; }
        public int? FrameworkId { get; set; }
        [Required(ErrorMessage = "Nhập tiêu đề"), MaxLength(255)] public string Title { get; set; } = "";
        [Required(ErrorMessage = "Nhập slug"), MaxLength(255)] public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public decimal ListPrice { get; set; }
        public decimal? SalePrice { get; set; }
        public bool IsPurchasable { get; set; } = true;
        public int? AccessDurationDays { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class CourseVersionDto
    {
        public int CourseVersionId { get; set; }
        public int CourseId { get; set; }
        public int VersionNumber { get; set; }
        public string? Label { get; set; }
        public VersionState State { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewCommentDto
    {
        public int CommentId { get; set; }
        public int? NodeId { get; set; }
        public int? BlockId { get; set; }
        public string Body { get; set; } = "";
        public CommentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ContentReviewDto
    {
        public int ReviewId { get; set; }
        public int CourseVersionId { get; set; }
        public int ReviewerId { get; set; }
        public ReviewDecision Decision { get; set; }
        public string? Summary { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ReviewCommentDto> Comments { get; set; } = new();
    }

    // ---------- content tree ----------
    public class ContentNodeDto
    {
        public int NodeId { get; set; }
        public int CourseVersionId { get; set; }
        public int? ParentNodeId { get; set; }
        public NodeType NodeType { get; set; }
        public string Title { get; set; } = "";
        public string? Slug { get; set; }
        public int OrderIndex { get; set; }
        public int Depth { get; set; }
        public bool IsFree { get; set; }
        public bool IsHidden { get; set; }
        public int? DurationMinutes { get; set; }
        public List<ContentNodeDto> Children { get; set; } = new();
    }

    public class ContentBlockDto
    {
        public int BlockId { get; set; }
        public int NodeId { get; set; }
        public LessonBlockType BlockType { get; set; }
        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public string? MetadataJson { get; set; }
        public int OrderIndex { get; set; }
    }

    public class ContentNodeDetailDto : ContentNodeDto
    {
        public List<ContentBlockDto> Blocks { get; set; } = new();
    }

    // ---------- page view models ----------
    public class CourseListVm : AdminListVmBase
    {
        public List<CourseAdminDto> Courses { get; set; } = new();
        public List<CatalogRefDto> Subjects { get; set; } = new();
        public List<CatalogRefDto> Grades { get; set; } = new();
        public int? SubjectId { get; set; }
        public int? GradeLevelId { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)) : 1;
    }

    public class CourseDetailVm
    {
        public CourseAdminDto Course { get; set; } = new();
        public List<CourseVersionDto> Versions { get; set; } = new();
    }

    public class VersionWorkspaceVm
    {
        public CourseAdminDto Course { get; set; } = new();
        public CourseVersionDto Version { get; set; } = new();
        public List<ContentNodeDto> Tree { get; set; } = new();
        public List<ContentReviewDto> Reviews { get; set; } = new();
        public ContentNodeDetailDto? SelectedNode { get; set; }
    }

    public class CatalogPageVm
    {
        public List<CatalogItemDto> Subjects { get; set; } = new();
        public List<CatalogItemDto> Grades { get; set; } = new();
        public List<CatalogItemDto> Frameworks { get; set; } = new();
    }

    /// <summary>Model cho partial cây nội dung đệ quy <c>_NodeTree.cshtml</c>.</summary>
    public class NodeTreeVm
    {
        public List<ContentNodeDto> Nodes { get; set; } = new();
        public int VersionId { get; set; }
        public int CourseId { get; set; }
        public int? SelectedNodeId { get; set; }
    }

    // ============================================================
    //  Import khung chương trình từ file CSV (api/content/import)
    // ============================================================

    public enum ImportIssueSeverity { Error, Warning }

    public class ImportIssueDto
    {
        public string File { get; set; } = "";
        public int? Row { get; set; }
        public string? NodeKey { get; set; }
        public string Code { get; set; } = "";
        public ImportIssueSeverity Severity { get; set; }
        public string Message { get; set; } = "";
    }

    public class ContentImportCountsDto
    {
        public int Chapters { get; set; }
        public int Lessons { get; set; }
        public int OtherNodes { get; set; }
        public int Blocks { get; set; }
        public int FlashcardDecks { get; set; }
        public int Flashcards { get; set; }
        public int Resources { get; set; }
    }

    public class ContentImportResultDto
    {
        public bool Valid { get; set; }
        public bool Committed { get; set; }
        public bool DryRun { get; set; }
        public int? ImportJobId { get; set; }
        public int? CourseId { get; set; }
        public int? CourseVersionId { get; set; }
        public ContentImportCountsDto Counts { get; set; } = new();
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public List<ImportIssueDto> Issues { get; set; } = new();
    }

    public class ContentImportJobDto
    {
        public int ImportJobId { get; set; }
        public string? UploadedByName { get; set; }
        public string FileUrl { get; set; } = "";
        public string TargetType { get; set; } = "";
        public int? CourseVersionId { get; set; }
        public string Status { get; set; } = "";
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Dữ liệu form upload trên trang Import.</summary>
    public class ContentImportUploadVm
    {
        public IFormFile? Course { get; set; }
        public IFormFile? Nodes { get; set; }
        public IFormFile? Blocks { get; set; }
        public IFormFile? Flashcards { get; set; }
        public IFormFile? Resources { get; set; }

        /// <summary>"new" = tạo khoá học mới từ course.csv · "version" = import vào một CourseVersion Draft.</summary>
        public string Mode { get; set; } = "new";
        public int? VersionId { get; set; }
        public bool Replace { get; set; }
        public bool PublishNow { get; set; }
        public bool ValidateOnly { get; set; }
    }

    public class ContentImportPageVm
    {
        public ContentImportUploadVm Form { get; set; } = new();
        public ContentImportResultDto? Result { get; set; }
        public List<ContentImportJobDto> RecentJobs { get; set; } = new();

        /// <summary>Kết quả chạy submit → duyệt → xuất bản (khi tích "Xuất bản ngay").</summary>
        public string? PublishSummary { get; set; }
        public int? CreatedCourseId { get; set; }

        public static string IssueSeverityLabel(ImportIssueSeverity s)
            => s == ImportIssueSeverity.Error ? "Lỗi" : "Cảnh báo";
    }
}
