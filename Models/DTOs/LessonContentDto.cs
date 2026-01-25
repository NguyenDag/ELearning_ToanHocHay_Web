
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Models.DTOs
{

    public class LessonContentDto
    {
        public int ContentId { get; set; }
        public LessonBlockType BlockType { get; set; }
        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
    }

    public class CreateLessonContentDto
    {
        //public int LessonId { get; set; }
        public LessonBlockType BlockType { get; set; }
        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
    }

    public class UpdateLessonContentDto
    {
        public int LessonId { get; set; }
        public LessonBlockType BlockType { get; set; }
        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
    }
}
