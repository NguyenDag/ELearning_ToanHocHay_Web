using System.Text.Json.Serialization;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        FillBlank,
        Essay
    }

    // (Có thể giữ hoặc bỏ Enum DifficultyLevel nếu chuyển sang dùng int Level)
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    public enum QuestionStatus
    {
        Draft,
        PendingReview,
        Approved,
        Rejected
    }
    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public QuestionType QuestionType { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public List<QuestionOptionDto>? Options { get; set; }
    }
}
