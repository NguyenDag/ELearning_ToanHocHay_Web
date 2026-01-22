using System.Text.Json.Serialization;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public class ExerciseDetailDto
    {
        public int ExerciseId { get; set; }

        [JsonPropertyName("exerciseName")]
        public string Title { get; set; }

        [JsonPropertyName("durationMinutes")]
        public int? DurationMinutes { get; set; }

        [JsonPropertyName("questions")]
        public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
    }

    

}