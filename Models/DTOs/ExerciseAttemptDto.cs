using System;
using System.Collections.Generic;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public enum AttemptStatus
    {
        InProgress,
        Submitted,
        Timeout
    }

    /// <summary>
    /// DTO chứa thông tin lượt làm bài trả về từ API cho WebApp.
    /// Backend serialize enum thành CHUỖI ("Test", "InProgress", "MultipleChoice") — các trường
    /// enum phải khai báo đúng kiểu enum, KHÔNG dùng int (deserialize "Test" -> int sẽ ném lỗi
    /// và khiến "Bắt đầu làm bài" báo lỗi hệ thống).
    /// </summary>
    public class ExerciseAttemptDto
    {
        public int AttemptId { get; set; }
        public int StudentId { get; set; }
        public int ExerciseId { get; set; }
        public string? ExerciseName { get; set; }
        public ExerciseType ExerciseType { get; set; }
        public DateTime StartTime { get; set; }
        // Thời điểm PHẢI kết thúc (đếm giờ). Null với bài không giới hạn thời gian.
        public DateTime? PlannedEndTime { get; set; }

        // Thời điểm thực sự nộp bài (null nếu chưa submit)
        public DateTime? SubmittedAt { get; set; }

        public AttemptStatus Status { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsCompleted { get; set; }

        // Danh sách câu hỏi trong lượt làm bài
        public List<QuestionInAttemptDto> Questions { get; set; } = new();
        public double Score { get; set; }

    }

    public class QuestionInAttemptDto
    {
        public int QuestionId { get; set; }
        public string? QuestionText { get; set; }
        public QuestionType QuestionType { get; set; }
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