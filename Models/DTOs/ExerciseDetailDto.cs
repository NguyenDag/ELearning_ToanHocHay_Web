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

    public class QuestionDto
    {
        public int QuestionId { get; set; }

        // SỬA: Khớp với trường "questionText" trong JSON Backend đang trả về
        [JsonPropertyName("questionText")]
        public string Content { get; set; }

        [JsonPropertyName("options")]
        public List<OptionDto> Options { get; set; } = new List<OptionDto>();
    }

    public class OptionDto
    {
        [JsonPropertyName("optionId")]
        public int OptionId { get; set; }

        // SỬA: Khớp với trường "optionText" trong JSON Backend đang trả về
        [JsonPropertyName("optionText")]
        public string Content { get; set; }
    }
}