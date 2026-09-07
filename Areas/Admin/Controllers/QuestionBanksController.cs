using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Admin;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    public class QuestionBanksController : AdminBaseController
    {
        private readonly QuestionBankAdminApiService _banks;
        private readonly CatalogAdminApiService _catalog;

        public QuestionBanksController(QuestionBankAdminApiService banks, CatalogAdminApiService catalog)
        {
            _banks = banks;
            _catalog = catalog;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? subjectId, int? gradeLevelId)
        {
            var vm = new QuestionBankListVm
            {
                Banks = await _banks.ListBanksAsync(subjectId, gradeLevelId),
                Subjects = await _catalog.GetSubjectsAsync(),
                Grades = await _catalog.GetGradeLevelsAsync()
            };
            ViewBag.SubjectId = subjectId;
            ViewBag.GradeLevelId = gradeLevelId;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await FillCatalogAsync();
            return View(new QuestionBankVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuestionBankVm vm)
        {
            if (!ModelState.IsValid) { await FillCatalogAsync(); return View(vm); }
            var r = await _banks.CreateBankAsync(vm);
            if (r.IsSuccess)
            {
                this.PushToastSuccess("Đã tạo ngân hàng câu hỏi.");
                return RedirectToAction(nameof(Questions), new { bankId = r.Data!.BankId });
            }
            this.ShowToastError(r);
            await FillCatalogAsync();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int bankId)
        {
            var b = await _banks.GetBankAsync(bankId);
            if (b == null) { this.PushToastError("Không tìm thấy ngân hàng."); return RedirectToAction(nameof(Index)); }
            await FillCatalogAsync();
            ViewBag.BankId = bankId;
            return View(new QuestionBankVm
            {
                BankName = b.BankName,
                Description = b.Description,
                SubjectId = b.SubjectId,
                GradeLevelId = b.GradeLevelId,
                CourseId = b.CourseId,
                PrimaryNodeId = b.PrimaryNodeId,
                IsActive = b.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int bankId, QuestionBankVm vm)
        {
            if (!ModelState.IsValid) { await FillCatalogAsync(); ViewBag.BankId = bankId; return View(vm); }
            this.PushToastResult(await _banks.UpdateBankAsync(bankId, vm), "Đã cập nhật ngân hàng.");
            return RedirectToAction(nameof(Questions), new { bankId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int bankId)
        {
            var r = await _banks.DeleteBankAsync(bankId);
            this.PushToastResult(r, "Đã xoá ngân hàng câu hỏi.");
            return RedirectToAction(nameof(Index));
        }

        // ---------- questions in a bank ----------

        [HttpGet]
        public async Task<IActionResult> Questions(int bankId, QuestionStatus? status, string? search, int page = 1)
        {
            var bank = await _banks.GetBankAsync(bankId);
            if (bank == null) { this.PushToastError("Không tìm thấy ngân hàng."); return RedirectToAction(nameof(Index)); }
            var vm = await _banks.ListQuestionsAsync(bankId, status, search, page < 1 ? 1 : page, 20);
            vm.Bank = bank;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateQuestion(int bankId)
        {
            var bank = await _banks.GetBankAsync(bankId);
            if (bank == null) { this.PushToastError("Không tìm thấy ngân hàng."); return RedirectToAction(nameof(Index)); }
            ViewBag.Bank = bank;
            return View("QuestionForm", new QuestionEditVm { BankId = bankId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestion(QuestionEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Bank = await _banks.GetBankAsync(vm.BankId);
                return View("QuestionForm", vm);
            }
            var r = await _banks.CreateQuestionAsync(vm);
            this.PushToastResult(r, "Đã tạo câu hỏi.");
            return RedirectToAction(nameof(Questions), new { bankId = vm.BankId });
        }

        [HttpGet]
        public async Task<IActionResult> EditQuestion(int questionId)
        {
            var q = await _banks.GetQuestionAsync(questionId);
            if (q == null) { this.PushToastError("Không tìm thấy câu hỏi."); return RedirectToAction(nameof(Index)); }
            ViewBag.Bank = await _banks.GetBankAsync(q.BankId);
            ViewBag.Question = q;
            var opts = q.Options ?? new();
            return View("QuestionForm", new QuestionEditVm
            {
                QuestionId = q.QuestionId,
                BankId = q.BankId,
                QuestionText = q.QuestionText,
                QuestionImageUrl = q.QuestionImageUrl,
                QuestionType = q.QuestionType,
                DifficultyLevel = q.DifficultyLevel,
                CorrectAnswer = q.CorrectAnswer,
                Explanation = q.Explanation,
                OptionTexts = opts.Select(o => o.OptionText).ToList(),
                CorrectIndexes = opts.Select((o, i) => (o, i)).Where(x => x.o.IsCorrect).Select(x => x.i).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int questionId, QuestionEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Bank = await _banks.GetBankAsync(vm.BankId);
                return View("QuestionForm", vm);
            }
            this.PushToastResult(await _banks.UpdateQuestionAsync(questionId, vm), "Đã cập nhật câu hỏi.");
            return RedirectToAction(nameof(Questions), new { bankId = vm.BankId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuestion(int questionId, int bankId)
        {
            this.PushToastResult(await _banks.SubmitQuestionAsync(questionId), "Đã gửi câu hỏi để duyệt.");
            return RedirectToAction(nameof(Questions), new { bankId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewQuestion(int questionId, int bankId, bool approve, string? rejectReason)
        {
            this.PushToastResult(await _banks.ReviewQuestionAsync(questionId, approve, rejectReason),
                approve ? "Đã duyệt câu hỏi." : "Đã từ chối câu hỏi.");
            return RedirectToAction(nameof(Questions), new { bankId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int questionId, int bankId)
        {
            this.PushToastResult(await _banks.DeleteQuestionAsync(questionId), "Đã xoá câu hỏi.");
            return RedirectToAction(nameof(Questions), new { bankId });
        }

        private async Task FillCatalogAsync()
        {
            ViewBag.Subjects = await _catalog.GetSubjectsAsync();
            ViewBag.Grades = await _catalog.GetGradeLevelsAsync();
        }
    }
}
