using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public class AIInsightResponse
    {
        [JsonPropertyName("concepts_to_review")]
        public List<string> ConceptsToReview { get; set; } = new();

        [JsonPropertyName("recommended_exercises")]
        public List<string> RecommendedExercises { get; set; } = new();

        [JsonPropertyName("quick_tips")]
        public List<string> QuickTips { get; set; } = new();

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("lesson_id")]
        public int? LessonId { get; set; }
    }
}
