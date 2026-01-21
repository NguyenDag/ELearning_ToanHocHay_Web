using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;

namespace ToanHocHay.WebApp.Controllers
{
    public class ExamController : Controller
    {
        private readonly ExamApiService _examService;

        public ExamController(ExamApiService examService)
        {
            _examService = examService;
        }

        // 1. Trang danh sách bài thi (Khắc phục lỗi 404)
        public IActionResult Index()
        {
            return View();
        }

        // 2. Trang làm bài thi (Dynamic Data)
        // URL: /Exam/DoExam/1
        [HttpGet]
        public async Task<IActionResult> DoExam(int id)
        {
            // Nếu id = 0 nghĩa là link từ View truyền sang bị sai
            if (id <= 0) return BadRequest("Mã đề thi không hợp lệ.");

            var exam = await _examService.GetExerciseById(id);

            if (exam == null)
            {
                // Debug xem id nhận được là bao nhiêu
                System.Diagnostics.Debug.WriteLine($"---> KHÔNG TÌM THẤY EXERCISE VỚI ID: {id}");
                return NotFound("Không tìm thấy đề thi hoặc lỗi kết nối API.");
            }

            int studentId = 9;
            int attemptId = await _examService.StartExercise(id, studentId);

            if (attemptId == 0) return BadRequest("Không thể bắt đầu lượt làm bài mới (Backend từ chối).");

            ViewData["AttemptId"] = attemptId;
            return View(exam);
        }

        // 3. Xử lý nộp bài (Gọi từ Ajax bên View)
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitExamPayload payload)
        {
            if (payload == null || payload.AttemptId == 0)
                return BadRequest("Dữ liệu nộp bài không hợp lệ.");

            // Bước A: Gửi từng câu trả lời về Backend
            // Sử dụng Task.WhenAll để gửi song song tất cả đáp án thay vì gửi lần lượt (giúp chạy nhanh hơn)
            var tasks = payload.Answers.Select(ans => _examService.SubmitSingleAnswer(new SubmitAnswerRequestDto
            {
                AttemptId = payload.AttemptId,
                QuestionId = ans.QuestionId,
                SelectedOptionId = ans.SelectedOptionId
            }));

            await Task.WhenAll(tasks); // Đợi tất cả request gửi xong

            // Bước B: Gọi API Complete để chốt điểm và kết thúc bài thi
            var result = await _examService.CompleteExercise(payload.AttemptId);

            if (result)
            {
                return Ok(new { message = "Nộp bài thành công!" });
            }

            return BadRequest("Lỗi khi hoàn tất bài thi.");
        }

        // 4. Trang kết quả
        // 4. Trang kết quả (Sửa lại để nhận dữ liệu thật)
        [HttpGet]
        public async Task<IActionResult> Result(int attemptId)
        {
            // Gọi API lấy kết quả thực tế dựa trên attemptId
            var result = await _examService.GetExerciseResult(attemptId);

            if (result == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Truyền dữ liệu thật vào View
            return View(result);
        }
    }
}