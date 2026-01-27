using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanHocHay.WebApp.Models.DTOs
{
    /// <summary>
    /// Định nghĩa các trạng thái của chương trình học
    /// </summary>
    public enum CurriculumStatus
    {
        Draft = 0,      // Bản nháp
        Published = 1,  // Đã xuất bản
        Archived = 2    // Đã lưu trữ
    }

    public class CurriculumDto
    {
        public int CurriculumId { get; set; }

        public int GradeLevel { get; set; }

        public string Subject { get; set; } = null!;

        public string CurriculumName { get; set; } = null!;

        public string? Description { get; set; }

        public CurriculumStatus Status { get; set; }

        public int Version { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Thêm danh sách Chapters để hiển thị ở Index.cshtml
        public List<ChapterDto> Chapters { get; set; } = new List<ChapterDto>();
    }

    public class ChapterDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = null!;
        public int OrderIndex { get; set; }
        public List<TopicDto> Topics { get; set; } = new List<TopicDto>();
    }

    public class TopicDto
    {
        public int TopicId { get; set; }
        public string TopicName { get; set; } = null!;
        public int OrderIndex { get; set; }
        public List<LessonDto> Lessons { get; set; } = new List<LessonDto>();
    }

    


    public class CreateCurriculumDto
    {
        [Required]
        [Range(6, 12)]
        public int GradeLevel { get; set; }

        [Required, MaxLength(100)]
        public string Subject { get; set; } = null!;

        [Required, MaxLength(255)]
        public string CurriculumName { get; set; } = null!;

        public string? Description { get; set; }

        public CurriculumStatus Status { get; set; } = CurriculumStatus.Draft;
    }

    public class UpdateCurriculumDto
    {
        [Required]
        [Range(6, 12)]
        public int GradeLevel { get; set; }

        [Required, MaxLength(100)]
        public string Subject { get; set; } = null!;

        [Required, MaxLength(255)]
        public string CurriculumName { get; set; } = null!;

        public string? Description { get; set; }

        public CurriculumStatus Status { get; set; }
    }
}