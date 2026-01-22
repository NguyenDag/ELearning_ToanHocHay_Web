using System.Text.Json.Serialization;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public enum ExerciseType
    {
        Practice,
        Quiz,
        Test,
        Exam
    }

    public enum ExerciseStatus
    {
        Draft,
        Published,
        Archived
    }
    public class ExerciseDto
    {
        [JsonPropertyName("ExerciseId")]
        public int ExerciseId { get; set; }

        [JsonPropertyName("ExerciseName")]
        public string ExerciseName { get; set; } = null!;

        public ExerciseType ExerciseType { get; set; }

        [JsonPropertyName("TotalQuestions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("DurationMinutes")]
        public int? DurationMinutes { get; set; }

        [JsonPropertyName("IsFree")]
        public bool IsFree { get; set; }
        public bool IsActive { get; set; } = false;
        public double TotalPoints { get; set; }
        public double PassingScore { get; set; }

        public ExerciseStatus Status { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
    }
}
