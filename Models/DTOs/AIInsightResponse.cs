using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public class AIInsightResponse
    {
        public List<string> ConceptsToReview { get; set; } = new();
        public List<string> RecommendedExercises { get; set; } = new();
        public List<string> QuickTips { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
    }
}
