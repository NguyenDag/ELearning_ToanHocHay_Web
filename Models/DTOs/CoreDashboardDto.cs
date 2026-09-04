using System;
using System.Collections.Generic;
namespace ToanHocHay.WebApp.Models.DTOs
{
    public enum TrendDirection { Up, Down, Same }

    // Cấu trúc dữ liệu chuẩn để hứng JSON từ Backend
    public class CoreDashboardDto
    {
        public StudentInfoDto StudentInfo { get; set; }
        public OverviewStatsDto Stats { get; set; }
        public List<RecentLessonDto> RecentLessons { get; set; }
        public List<ChapterProgressSummaryDto> ChapterProgress { get; set; }
        public PackageTier PackageTier { get; set; }
        public DashboardLinksDto Links { get; set; }

        // ── THÊM MỚI ──────────────────────────────────────────────
        public SubscriptionInfoDto SubscriptionInfo { get; set; } = new();
        // ────────────────────────────────────────────────────────────
    }

    public class StudentInfoDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public int GradeLevel { get; set; }
        public string SchoolName { get; set; }
    }
    public class OverviewStatsDto
    {
        public int WeeklyStudyMinutes { get; set; }
        public int WeeklyExercisesCompleted { get; set; }
        public double AverageScore { get; set; }
        public int TotalExercisesCompleted { get; set; }
        public int TotalLessonsCompleted { get; set; }
        public ComparisonDto WeekComparison { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public bool StudiedToday { get; set; }
    }
    public class ComparisonDto
    {
        public int ScoreChange { get; set; }
        public int StudyTimeChange { get; set; }
        public int ExerciseCountChange { get; set; }
        public TrendDirection Direction { get; set; }
    }
    public class RecentLessonDto
    {
        public int LessonId { get; set; }
        public string LessonName { get; set; }
        public string TopicName { get; set; }
        public string ChapterName { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? DurationMinutes { get; set; }
        public bool IsCompleted { get; set; }
        public int ProgressPercentage { get; set; }
        public double? Score { get; set; }
        public int? AttemptId { get; set; }
        public int? TabSwitchCount { get; set; }
    }
    public class ChapterProgressSummaryDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; }
        public int OrderIndex { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int CompletedTopics { get; set; }
        public int TotalTopics { get; set; }
        public bool IsLocked { get; set; }
        public string? CurrentMastery { get; set; }   // NotStarted | Beginner | Intermediate | Advanced | Mastered
    }
    public class DashboardLinksDto
    {
        public string ExerciseHistory { get; set; }
        public string Charts { get; set; }
        public string AIInsights { get; set; }
        public string Notifications { get; set; }
    }
}