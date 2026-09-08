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
        public async Task<IActionResult> Index(int? subjectId, int? gradeLevelId, int page = 1, int pageSize = 20)
        {
            page = AdminPaging.NormalizePage(page);
            pageSize = AdminPaging.NormalizeSize(pageSize);
            var (all, status, error) = await _banks.ListBanksAsync(subjectId, gradeLevelId);
            var slice = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var vm = new QuestionBankListVm
            {
                Banks = slice,
                Total = all.Count,
                Page = page,
                PageSize = pageSize,
                SubjectId = subjectId,
                GradeLevelId = gradeLevelId,
                Subjects = await _catalog.GetSubjectsAsync(),
                Grades = await _catalog.GetGradeLevelsAsync()
            };
            if (error != null) vm.SetError(status, error);
            if (GuardListError(vm) is { } redirect) return redirect;
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

        // ---------- import câu hỏi / đề ----------

        [HttpGet]
        public async Task<IActionResult> Import(int? bankId)
        {
            var vm = new QuestionImportPageVm
            {
                Form = new QuestionImportUploadVm { BankId = bankId },
                Subjects = await _catalog.GetSubjectsAsync(),
                Grades = await _catalog.GetGradeLevelsAsync()
            };
            if (bankId is > 0)
            {
                vm.TargetBank = await _banks.GetBankAsync(bankId.Value);
                if (vm.TargetBank == null) { this.PushToastError("Không tìm thấy ngân hàng."); return RedirectToAction(nameof(Index)); }
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 20_000_000)]
        public async Task<IActionResult> Import(QuestionImportUploadVm form)
        {
            var vm = new QuestionImportPageVm
            {
                Form = form,
                Subjects = await _catalog.GetSubjectsAsync(),
                Grades = await _catalog.GetGradeLevelsAsync()
            };
            if (form.BankId is > 0) vm.TargetBank = await _banks.GetBankAsync(form.BankId.Value);

            var guard =
                form.Questions == null && form.Exercises == null ? "Cần chọn ít nhất questions.csv."
                : form.BankId is not > 0 && (form.SubjectId is not > 0 || form.GradeLevelId is not > 0)
                    ? "Chọn Môn và Lớp cho ngân hàng mới, hoặc chọn một ngân hàng có sẵn để thêm vào."
                    : null;
            if (guard != null) { this.ShowToastError(guard); return View(vm); }

            var r = await _banks.ImportAsync(form);
            if (this.AuthRedirectOrNull(r) is { } redirect) return redirect;
            vm.Result = r.Data;

            if (r.Data == null) this.ShowToastError(r);
            else if (!r.Data.Valid) this.ShowToastError($"File có {r.Data.ErrorCount} lỗi cần sửa.");
            else if (form.ValidateOnly)
                this.ShowToastSuccess($"File hợp lệ — {r.Data.Counts.Questions} câu hỏi, {r.Data.Counts.Exercises} bài tập.");
            else if (r.Data.Committed)
            {
                var c = r.Data.Counts;
                this.PushToastSuccess($"Đã import {c.Questions} câu hỏi" + (c.Exercises > 0 ? $" · {c.Exercises} bài tập" : "")
                    + (r.Data.WarningCount > 0 ? $" ({r.Data.WarningCount} cảnh báo)" : "") + ".");
                // về trang ngân hàng: đích cụ thể nếu thêm vào bank có sẵn, ngược lại danh sách
                return form.BankId is > 0
                    ? RedirectToAction(nameof(Questions), new { bankId = form.BankId })
                    : RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        // ---------- questions in a bank ----------

        [HttpGet]
        public async Task<IActionResult> Questions(int bankId, QuestionStatus? status, string? search, int page = 1, int pageSize = 20)
        {
            var bank = await _banks.GetBankAsync(bankId);
            if (bank == null) { this.PushToastError("Không tìm thấy ngân hàng."); return RedirectToAction(nameof(Index)); }
            var vm = await _banks.ListQuestionsAsync(bankId, status, search,
                AdminPaging.NormalizePage(page), AdminPaging.NormalizeSize(pageSize));
            vm.Bank = bank;
            if (GuardListError(vm) is { } redirect) return redirect;
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
