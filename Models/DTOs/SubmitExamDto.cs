namespace ToanHocHay.WebApp.Models.DTOs
{
    // DTO gửi từ View lên Controller
    public class SubmitExamPayload
    {
        public int AttemptId { get; set; }
        public List<StudentAnswerDto> Answers { get; set; } = new List<StudentAnswerDto>();
    }

    public class StudentAnswerDto
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public string? AnswerText { get; set; }     // THÊM DÒNG NÀY
    }

    // DTO gửi xuống Backend API
    public class StartExerciseDto
    {
        public int ExerciseId { get; set; }
        public int StudentId { get; set; }
    }

    public class SubmitAnswerRequestDto
    {
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public int? SelectedOptionId { get; set; }
    }

    public class CompleteExerciseDto
    {
        public int AttemptId { get; set; }
    }

    // DTO nhận kết quả Start
    
    public class ExerciseAttemptResponseDto
    {
        public int AttemptId { get; set; }
        // Các trường khác nếu cần...
    }
}