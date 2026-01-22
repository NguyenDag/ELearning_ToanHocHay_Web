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
        public int SelectedOptionId { get; set; }
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
        public int? SelectedOptionId { get; set; }
    }

    public class CompleteExerciseDto
    {
        public int AttemptId { get; set; }
    }

    // DTO nhận kết quả Start
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
    }

    public class ExerciseAttemptResponseDto
    {
        public int AttemptId { get; set; }
        // Các trường khác nếu cần...
    }
}