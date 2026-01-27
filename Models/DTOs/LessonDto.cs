using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanHocHay.WebApp.Models.DTOs
{
    // ============================================================
    // 1. NHÓM DTO CHO BÀI HỌC (LESSON)
    // ============================================================

    public class LessonDto
    {
        public int LessonId { get; set; }
        public int TopicId { get; set; }
        public string LessonName { get; set; } = null!;
        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }

        // Kiểu LessonStatus này sẽ được tự động nhận diện từ file LessonStatus.cs
        public LessonStatus Status { get; set; }

        public List<LessonContentDto> Contents { get; set; } = new List<LessonContentDto>();
    }

    public class CreateLessonDto
    {
        [Required]
        public int TopicId { get; set; }

        [Required, MaxLength(255)]
        public string LessonName { get; set; } = null!;

        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
    }

    public class UpdateLessonDto
    {
        [Required, MaxLength(255)]
        public string LessonName { get; set; } = null!;

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

    // ============================================================
    // 2. NHÓM DTO CHO NỘI DUNG BÀI HỌC (LESSON CONTENT)
    // ============================================================

    public class LessonContentDto
    {
        public int ContentId { get; set; }

        // Kiểu LessonBlockType này sẽ được tự động nhận diện từ file LessonBlockType.cs
        public LessonBlockType BlockType { get; set; }

        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
    }

    public class CreateLessonContentDto
    {
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