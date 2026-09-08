using System.ComponentModel.DataAnnotations;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Areas.Admin.Models
{
    public static class QuestionEnums
    {
        public static string TypeLabel(QuestionType t) => t switch
        {
            QuestionType.MultipleChoice => "Trắc nghiệm",
            QuestionType.TrueFalse => "Đúng / Sai",
            QuestionType.FillBlank => "Điền khuyết",
            QuestionType.Essay => "Tự luận",
            _ => t.ToString()
        };
        public static string DifficultyLabel(DifficultyLevel d) => d switch
        {
            DifficultyLevel.Easy => "Dễ", DifficultyLevel.Medium => "Trung bình", DifficultyLevel.Hard => "Khó", _ => d.ToString()
        };
        public static string StatusLabel(QuestionStatus s) => s switch
        {
            QuestionStatus.Draft => "Nháp",
            QuestionStatus.PendingReview => "Chờ duyệt",
            QuestionStatus.Approved => "Đã duyệt",
            QuestionStatus.Rejected => "Từ chối",
            _ => s.ToString()
        };
    }

    public class QuestionBankDto
    {
        public int BankId { get; set; }
        public string BankName { get; set; } = "";
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int GradeLevelId { get; set; }
        public string? GradeLevelName { get; set; }
        public int? CourseId { get; set; }
        public int? PrimaryNodeId { get; set; }
        public bool IsActive { get; set; }
        public int QuestionCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class QuestionBankVm
    {
        [Required(ErrorMessage = "Nhập tên ngân hàng"), MaxLength(255)]
        public string BankName { get; set; } = "";
        public string? Description { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Chọn môn")]
        public int SubjectId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Chọn lớp")]
        public int GradeLevelId { get; set; }
        public int? CourseId { get; set; }
        public int? PrimaryNodeId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AdminQuestionDto
    {
        public int QuestionId { get; set; }
        public int BankId { get; set; }
        public int SubjectId { get; set; }
        public string QuestionText { get; set; } = "";
        public string? QuestionImageUrl { get; set; }
        public QuestionType QuestionType { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public QuestionStatus Status { get; set; }
        public bool IsActive { get; set; }
        public string? RejectReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }

    /// <summary>Form soạn / sửa 1 câu hỏi (dùng cho cả tạo mới và cập nhật).</summary>
    public class QuestionEditVm
    {
        public int QuestionId { get; set; }
        public int BankId { get; set; }
        [Required(ErrorMessage = "Nhập nội dung câu hỏi")]
        public string QuestionText { get; set; } = "";
        public string? QuestionImageUrl { get; set; }
        public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
        public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Easy;
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        /// <summary>Mỗi dòng một phương án (cho trắc nghiệm). Đánh dấu đáp án đúng bằng ô chọn.</summary>
        public List<string> OptionTexts { get; set; } = new() { "", "", "", "" };
        public List<int> CorrectIndexes { get; set; } = new();
    }

    public class QuestionBankListVm : AdminListVmBase
    {
        public List<QuestionBankDto> Banks { get; set; } = new();
        public List<CatalogRefDto> Subjects { get; set; } = new();
        public List<CatalogRefDto> Grades { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public int? SubjectId { get; set; }
        public int? GradeLevelId { get; set; }
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)) : 1;
    }

    public class QuestionListVm : AdminListVmBase
    {
        public QuestionBankDto Bank { get; set; } = new();
        public List<AdminQuestionDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public QuestionStatus? Status { get; set; }
        public string? Search { get; set; }
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)) : 1;
    }

    public class CatalogRefDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // ---------- import câu hỏi / đề ----------
    public class QuestionImportUploadVm
    {
        public IFormFile? QuestionBank { get; set; }
        public IFormFile? Questions { get; set; }
        public IFormFile? QuestionOptions { get; set; }
        public IFormFile? Exercises { get; set; }
        public IFormFile? ExerciseQuestions { get; set; }

        /// <summary>Có giá trị = thêm câu hỏi vào ngân hàng này; ngược lại tạo ngân hàng mới.</summary>
        public int? BankId { get; set; }
        public int? SubjectId { get; set; }
        public int? GradeLevelId { get; set; }
        public bool ValidateOnly { get; set; }
    }

    public class QuestionImportPageVm
    {
        public QuestionImportUploadVm Form { get; set; } = new();
        public ContentImportResultDto? Result { get; set; }
        public QuestionBankDto? TargetBank { get; set; }
        public List<CatalogRefDto> Subjects { get; set; } = new();
        public List<CatalogRefDto> Grades { get; set; } = new();
    }
}
