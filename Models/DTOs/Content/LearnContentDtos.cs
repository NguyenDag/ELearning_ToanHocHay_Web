using System;
using System.Collections.Generic;

namespace ToanHocHay.WebApp.Models.DTOs.Content
{
    /// <summary>Khớp <c>NodeType</c> của backend (serialize dạng chuỗi).</summary>
    public enum ContentNodeType { Chapter, Topic, SubTopic, Lesson }

    // ---------- catalog ----------
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? IconUrl { get; set; }
        public string? ColorHex { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class GradeLevelDto
    {
        public int GradeLevelId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    // ---------- courses ----------
    public class CourseSummaryDto
    {
        public int CourseId { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int GradeLevelId { get; set; }
        public string? GradeLevelName { get; set; }
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public decimal ListPrice { get; set; }
        public decimal? SalePrice { get; set; }
        public bool IsPurchasable { get; set; }
        public int DisplayOrder { get; set; }
        public int? PublishedVersionId { get; set; }
        public int? PublishedVersionNumber { get; set; }
    }

    // ---------- learn: cây nội dung + node chi tiết ----------
    public class ContentNodeDto
    {
        public int NodeId { get; set; }
        public int CourseVersionId { get; set; }
        public int? ParentNodeId { get; set; }
        public ContentNodeType NodeType { get; set; }
        public string Title { get; set; } = "";
        public string? Slug { get; set; }
        public int OrderIndex { get; set; }
        public int Depth { get; set; }
        public bool IsFree { get; set; }
        public bool IsHidden { get; set; }
        public int? DurationMinutes { get; set; }
        public List<ContentNodeDto> Children { get; set; } = new();
    }

    public class ContentNodeDetailDto : ContentNodeDto
    {
        public List<ContentBlockDto> Blocks { get; set; } = new();
        public List<LessonResourceDto> Resources { get; set; } = new();
        public List<FlashcardDeckDto> FlashcardDecks { get; set; } = new();
    }

    public class ContentBlockDto
    {
        public int BlockId { get; set; }
        public int NodeId { get; set; }
        public LessonBlockType BlockType { get; set; }
        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public string? MetadataJson { get; set; }
        public int OrderIndex { get; set; }
    }

    public class LessonResourceDto
    {
        public int ResourceId { get; set; }
        public int NodeId { get; set; }
        public string Title { get; set; } = "";
        public string ResourceType { get; set; } = "";   // Pdf | Slide | Doc | Sheet | ExternalLink
        public int? MediaAssetId { get; set; }
        public string? ExternalUrl { get; set; }
        public bool IsDownloadable { get; set; }
        public int OrderIndex { get; set; }
    }

    public class FlashcardDeckDto
    {
        public int DeckId { get; set; }
        public int NodeId { get; set; }
        public string Title { get; set; } = "";
        public List<FlashcardDto> Cards { get; set; } = new();
    }

    public class FlashcardDto
    {
        public int CardId { get; set; }
        public int DeckId { get; set; }
        public string FrontText { get; set; } = "";
        public string BackText { get; set; } = "";
        public string? FrontImageUrl { get; set; }
        public string? BackImageUrl { get; set; }
        public string? Hint { get; set; }
        public int OrderIndex { get; set; }
    }

    public class CourseContentDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public int CourseVersionId { get; set; }
        public int VersionNumber { get; set; }

        /// <summary>"Full" khi trả cây đầy đủ; "FreeOnly" khi chỉ có node miễn phí.</summary>
        public string AccessLevel { get; set; } = "";
        public bool IsEntitled { get; set; }
        public List<ContentNodeDto> Tree { get; set; } = new();
    }

    // ---------- enrolment & progress ----------
    public class EnrolmentDto
    {
        public int StudentCourseId { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public string? SubjectName { get; set; }
        public string? GradeLevelName { get; set; }
        public int CourseVersionId { get; set; }
        public string Source { get; set; } = "";   // Self | Assigned | Subscription | Purchase
        public string Status { get; set; } = "";   // Active | Completed | Expired | Cancelled
        public decimal ProgressPercent { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? AccessExpiresAt { get; set; }
    }

    public class NodeProgressDto
    {
        public int NodeId { get; set; }
        public ContentNodeType NodeType { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "";        // NotStarted | InProgress | Completed
        public string MasteryLevel { get; set; } = "";
        public decimal CompletionPercent { get; set; }
        public int TimeSpentSeconds { get; set; }
        public int TotalAttempts { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public DateTime LastAccessedAt { get; set; }

        public bool IsCompleted => string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase);
    }
}
