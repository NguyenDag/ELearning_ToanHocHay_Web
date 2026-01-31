using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Common;
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
        public async Task<IActionResult> Index()
        {
            var exams = await _examService.GetExercisesAsync();

            Console.WriteLine(exams.Count);

            return View(exams);
        }

        // 2. Trang làm bài thi (Dynamic Data)
        // URL: /Exam/DoExam/1
        [HttpGet]
        public async Task<IActionResult> DoExam(int id)
        {
            if (id <= 0) return BadRequest("Mã đề thi không hợp lệ.");

            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            // FIX QUAN TRỌNG: Tìm đúng chuỗi "StudentId" khớp với AccountController
            var studentIdClaim = User.FindFirst("StudentId");

            if (studentIdClaim == null)
            {
                // Nếu vào đây, nghĩa là Cookie của bro không có mã học sinh
                // Nguyên nhân: Do API trả về StudentId null HOẶC bro chưa đăng nhập lại sau khi sửa code
                return Unauthorized("Tài khoản không phải là học sinh.");
            }

            var exam = await _examService.GetExerciseById(id);
            if (exam == null) return NotFound("Không tìm thấy đề thi.");

            int studentId = int.Parse(studentIdClaim.Value);
            int attemptId = await _examService.StartExercise(id, studentId);

            if (attemptId == 0) return BadRequest("Không thể bắt đầu lượt làm bài. Kiểm tra log API để biết chi tiết.");

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