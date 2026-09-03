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

            var studentIdStr = User.FindFirst("StudentId")?.Value;
            if (!string.IsNullOrEmpty(studentIdStr))
            {
                var completedIds = await _examService.GetCompletedExerciseIdsAsync(int.Parse(studentIdStr));
                ViewData["CompletedIds"] = completedIds;
            }
            else
            {
                ViewData["CompletedIds"] = new List<int>();
            }

            return View(exams);
        }

        // 2. Trang làm bài thi (Dynamic Data)
        // URL: /Exam/DoExam/1
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> DoExam(int id)
        {
            if (id <= 0) return RedirectToAction("Index");
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var studentIdClaim = User.FindFirst("StudentId");
            if (studentIdClaim == null)
                return RedirectToAction("Index");

            var exam = await _examService.GetExerciseById(id);
            if (exam == null)
                return RedirectToAction("Index");

            // Chặn nhanh phía UI khi rõ ràng không đủ điều kiện; quyết định cuối vẫn ở backend
            // (StartExercise trả 403 theo Exercise.RequiredTier — xử lý ở nhánh attemptId == 0).
            if (!exam.IsFree)
            {
                var tier = User.FindFirst("PackageTier")?.Value ?? nameof(PackageTier.Free);
                if (string.Equals(tier, nameof(PackageTier.Free), StringComparison.OrdinalIgnoreCase))
                {
                    TempData["UpgradeMsg"] = "Đề thi này yêu cầu gói trả phí. Vui lòng nâng cấp để tiếp tục.";
                    return RedirectToAction("Index", "Package");
                }
            }

            int studentId = int.Parse(studentIdClaim.Value);
            var (attemptId, error) = await _examService.StartExercise(id, studentId);

            if (attemptId == 0)
            {
                TempData["ErrorMessage"] = error ?? "Lỗi dữ liệu không hợp lệ.";
                return RedirectToAction("Index");
            }

            ViewData["AttemptId"] = attemptId;
            ViewData["Time"] = exam.DurationMinutes.HasValue ? $"{exam.DurationMinutes}:00" : "15:00";
            return View(exam);
        }

        // 3. Xử lý nộp bài (Gọi từ Ajax bên View)
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitExamPayload payload)
        {
            if (payload == null || payload.AttemptId == 0)
                return BadRequest("Dữ liệu nộp bài không hợp lệ.");

            // Gửi từng câu trả lời — truyền đủ cả SelectedOptionId VÀ AnswerText
            var tasks = payload.Answers.Select(ans => _examService.SaveSingleAnswer(new SubmitAnswerRequestDto
            {
                AttemptId = payload.AttemptId,
                QuestionId = ans.QuestionId,
                SelectedOptionId = ans.SelectedOptionId,  // MultipleChoice
                AnswerText = ans.AnswerText          // TrueFalse + FillBlank ← THÊM DÒNG NÀY
            }));

            await Task.WhenAll(tasks);

            var result = await _examService.CompleteExercise(payload.AttemptId);

            return result
                ? Ok(new { message = "Nộp bài thành công!" })
                : BadRequest("Lỗi khi hoàn tất bài thi.");
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

            var tabSwitchLogs = await _examService.GetTabSwitchLogsAsync(attemptId);
            ViewBag.TabSwitchLogs = tabSwitchLogs;

            // Truyền dữ liệu thật vào View
            return View(result);
        }
        // 5. Gọi AI Gợi ý
        [HttpPost]
        public async Task<IActionResult> GetHint([FromBody] AIHintRequestDto payload)
        {
            if (payload == null) return BadRequest("Dữ liệu không hợp lệ.");

            var hint = await _examService.GetAIHintAsync(payload);
            if (hint != null)
            {
                return Ok(new { success = true, data = hint });
            }

            return BadRequest(new { success = false, message = "Không thể lấy gợi ý từ AI." });
        }

        // 6. Báo cáo chuyển tab - gửi thông báo cho phụ huynh
        [HttpPost]
        public async Task<IActionResult> ReportTabSwitch([FromBody] TabSwitchReportDto report)
        {
            if (report == null || report.AttemptId <= 0)
                return BadRequest("Dữ liệu không hợp lệ.");

            await _examService.ReportTabSwitchAsync(report.AttemptId);
            return Ok(new { success = true });
        }
    }
}