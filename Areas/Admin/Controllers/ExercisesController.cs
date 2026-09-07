using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Services.Admin;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    public class ExercisesController : AdminBaseController
    {
        private readonly ExerciseAdminApiService _ex;

        public ExercisesController(ExerciseAdminApiService ex) => _ex = ex;

        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20)
        {
            page = AdminPaging.NormalizePage(page);
            pageSize = AdminPaging.NormalizeSize(pageSize);
            var (all, status, error) = await _ex.ListAsync();

            IEnumerable<ExerciseAdminDto> q = all.OrderByDescending(e => e.CreatedAt);
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(e => e.ExerciseName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
            var list = q.ToList();

            var vm = new ExerciseListVm
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Total = list.Count,
                Page = page,
                PageSize = pageSize,
                Search = search
            };
            if (error != null) vm.SetError(status, error);
            if (GuardListError(vm) is { } redirect) return redirect;
            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
            => View("Edit", new ExerciseEditPageVm { Exercise = new ExerciseEditVm() });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExerciseEditVm exercise)
        {
            if (!ModelState.IsValid)
                return View("Edit", new ExerciseEditPageVm { Exercise = exercise });

            var r = await _ex.CreateAsync(exercise);
            if (r.IsSuccess)
            {
                this.PushToastSuccess("Đã tạo bài kiểm tra.");
                return RedirectToAction(nameof(Edit), new { id = r.Data!.ExerciseId });
            }
            this.ShowToastError(r);
            return View("Edit", new ExerciseEditPageVm { Exercise = exercise });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var e = await _ex.GetAsync(id);
            if (e == null) { this.PushToastError("Không tìm thấy bài kiểm tra."); return RedirectToAction(nameof(Index)); }
            return View(new ExerciseEditPageVm
            {
                Exercise = new ExerciseEditVm
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName,
                    ExerciseType = e.ExerciseType,
                    TopicId = e.TopicId,
                    ChapterId = e.ChapterId,
                    TotalQuestions = e.TotalQuestions,
                    DurationMinutes = e.DurationMinutes,
                    IsFree = e.IsFree,
                    IsActive = e.IsActive,
                    TotalScores = e.TotalPoints,
                    PassingScore = e.PassingScore,
                    Status = e.Status
                },
                Questions = await _ex.GetQuestionsAsync(id)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExerciseEditVm exercise)
        {
            if (!ModelState.IsValid)
                return View(new ExerciseEditPageVm { Exercise = exercise, Questions = await _ex.GetQuestionsAsync(id) });

            this.PushToastResult(await _ex.UpdateAsync(id, exercise), "Đã lưu bài kiểm tra.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            this.PushToastResult(await _ex.PublishAsync(id), "Đã xuất bản bài kiểm tra.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(int id)
        {
            this.PushToastResult(await _ex.UnpublishAsync(id), "Đã gỡ xuất bản.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _ex.DeleteAsync(id);
            this.PushToastResult(r, "Đã xoá bài kiểm tra.");
            return r.IsSuccess ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestions(int id, string questionIds, double? scorePerQuestion)
        {
            var ids = (questionIds ?? "")
                .Split(new[] { ',', ' ', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                .Where(n => n > 0)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
            {
                this.PushToastError("Nhập ít nhất một mã câu hỏi hợp lệ.");
                return RedirectToAction(nameof(Edit), new { id });
            }
            this.PushToastResult(await _ex.AddQuestionsAsync(id, ids, scorePerQuestion), $"Đã thêm {ids.Count} câu hỏi.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveQuestion(int id, int questionId)
        {
            this.PushToastResult(await _ex.RemoveQuestionAsync(id, questionId), "Đã gỡ câu hỏi.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetScore(int id, int questionId, double score)
        {
            this.PushToastResult(await _ex.SetScoreAsync(id, questionId, score), "Đã cập nhật điểm.");
            return RedirectToAction(nameof(Edit), new { id });
        }
    }
}
