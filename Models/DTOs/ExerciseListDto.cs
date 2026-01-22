using System.Text.Json.Serialization;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public class ExerciseListDto
    {
        [JsonPropertyName("ExerciseId")]
        public int ExerciseId { get; set; }

        [JsonPropertyName("ExerciseName")]
        public string ExerciseName { get; set; } = null!;

        [JsonPropertyName("TotalQuestions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("DurationMinutes")]
        public int? DurationMinutes { get; set; }

        [JsonPropertyName("IsFree")]
        public bool IsFree { get; set; }

        [JsonPropertyName("CreatedAt")]
        public DateTime CreatedAt { get; set; }
    }
}
