namespace ToanHocHay.WebApp.Models.DTOs
{
    public class AIInsightResponse
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> ConceptsToReview { get; set; } = new List<string>();
        public List<string> RecommendedExercises { get; set; } = new List<string>();
        public List<string> QuickTips { get; set; } = new List<string>();
        public int? LessonId { get; set; }
        public string Status { get; set; } = "success";
    }
}
