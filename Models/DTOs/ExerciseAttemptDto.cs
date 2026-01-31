using System;
using System.Collections.Generic;

namespace ToanHocHay.WebApp.Models.DTOs
{
    /// <summary>
    /// DTO chứa thông tin lượt làm bài trả về từ API cho WebApp
    /// </summary>
    public class ExerciseAttemptDto
    {
        public int AttemptId { get; set; }
        public int StudentId { get; set; }
        public int ExerciseId { get; set; }
        public string? ExerciseName { get; set; }
        public string? ExerciseType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsCompleted { get; set; }

        // Danh sách câu hỏi trong lượt làm bài
        public List<QuestionInAttemptDto> Questions { get; set; } = new();
    }

    public class QuestionInAttemptDto
    {
        public int QuestionId { get; set; }
        public string? QuestionText { get; set; }
        public string? QuestionType { get; set; }
        public double Score { get; set; }
        public string? ImageUrl { get; set; }
        public List<AnswerOptionDto> Options { get; set; } = new();
    }

    public class AnswerOptionDto
    {
        public int OptionId { get; set; }
        public string? OptionText { get; set; }
        public string? ImageUrl { get; set; }
    }
}