using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public class LessonDto
    {
        public int LessonId { get; set; }
        public int TopicId { get; set; }
        public string LessonName { get; set; }
        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
        public LessonStatus Status { get; set; }
        public List<LessonContentDto> Contents { get; set; } = new List<LessonContentDto>();
    }
    public class CreateLessonDto
    {
        [Required]
        public int TopicId { get; set; }

        [Required, MaxLength(255)]
        public string LessonName { get; set; }

        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
    }
    public class UpdateLessonDto
    {
        [Required, MaxLength(255)]
        public string LessonName { get; set; }

        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
    }
    public class ReviewLessonDto
    {
        public bool IsApproved { get; set; }
        public string? RejectReason { get; set; }
    }
}
