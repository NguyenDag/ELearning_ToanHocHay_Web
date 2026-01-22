using System.Text.Json.Serialization;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public class QuestionOptionDto
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; }
    }
}
