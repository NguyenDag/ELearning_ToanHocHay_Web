using System.ComponentModel.DataAnnotations;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Areas.Admin.Models
{
    public static class ExerciseEnums
    {
        public static string TypeLabel(ExerciseType t) => t switch
        {
            ExerciseType.Practice => "Luyện tập",
            ExerciseType.Quiz => "Kiểm tra nhanh",
            ExerciseType.Test => "Bài kiểm tra",
            ExerciseType.Exam => "Bài thi",
            _ => t.ToString()
        };
        public static string StatusLabel(ExerciseStatus s) => s switch
        {
            ExerciseStatus.Draft => "Nháp",
            ExerciseStatus.Published => "Đã xuất bản",
            ExerciseStatus.Archived => "Lưu trữ",
            _ => s.ToString()
        };
    }

    public class ExerciseAdminDto
    {
        public int ExerciseId { get; set; }
        public int? TopicId { get; set; }
        public int? ChapterId { get; set; }
        public string ExerciseName { get; set; } = "";
        public ExerciseType ExerciseType { get; set; }
        public int TotalQuestions { get; set; }
        public int? DurationMinutes { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
        public double TotalPoints { get; set; }
        public double PassingScore { get; set; }
        public ExerciseStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ExerciseEditVm
    {
        public int ExerciseId { get; set; }

        [Required(ErrorMessage = "Nhập tên bài kiểm tra"), MaxLength(255)]
        public string ExerciseName { get; set; } = "";
        public ExerciseType ExerciseType { get; set; } = ExerciseType.Quiz;
        public int? TopicId { get; set; }
        public int? ChapterId { get; set; }
        public int TotalQuestions { get; set; }
        public int? DurationMinutes { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
        public double TotalScores { get; set; }
        public double PassingScore { get; set; }
        public ExerciseStatus Status { get; set; } = ExerciseStatus.Draft;
    }

    public class ExerciseQuestionRowDto
    {
        public int QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public double Score { get; set; }
        public string QuestionText { get; set; } = "";
        public QuestionType QuestionType { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }

    public class ExerciseEditPageVm
    {
        public ExerciseEditVm Exercise { get; set; } = new();
        public List<ExerciseQuestionRowDto> Questions { get; set; } = new();
        public bool IsNew => Exercise.ExerciseId == 0;
    }

    public class ExerciseListVm : AdminListVmBase
    {
        public List<ExerciseAdminDto> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string? Search { get; set; }
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)) : 1;
    }
}
