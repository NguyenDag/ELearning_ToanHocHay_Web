using System.Text.Json.Serialization;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public class AIInsightResponse
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("concepts_to_review")]
        public List<string> ConceptsToReview { get; set; } = new List<string>();

        [JsonPropertyName("recommended_exercises")]
        public List<string> RecommendedExercises { get; set; } = new List<string>();

        [JsonPropertyName("quick_tips")]
        public List<string> QuickTips { get; set; } = new List<string>();

        [JsonPropertyName("lesson_id")]
        public int? LessonId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "success";
    }
}
