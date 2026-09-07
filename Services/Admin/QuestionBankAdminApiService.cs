using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Models.DTOs.Payment;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services.Admin
{
    public class QuestionBankAdminApiService
    {
        private readonly ApiClient _api;

        public QuestionBankAdminApiService(ApiClient api) => _api = api;

        // ---------- banks ----------
        public async Task<(List<QuestionBankDto> Items, int Status, string? Error)> ListBanksAsync(int? subjectId, int? gradeLevelId)
        {
            var qs = new List<string> { "includeInactive=true" };
            if (subjectId is > 0) qs.Add($"subjectId={subjectId}");
            if (gradeLevelId is > 0) qs.Add($"gradeLevelId={gradeLevelId}");
            var r = await _api.GetAsync<List<QuestionBankDto>>($"{ApiRoutes.QuestionBanks.List}?{string.Join('&', qs)}");
            return (r.Data ?? new(), r.StatusCode, r.IsSuccess ? null : r.DisplayMessage);
        }

        public async Task<QuestionBankDto?> GetBankAsync(int bankId)
        {
            var r = await _api.GetAsync<QuestionBankDto>(ApiRoutes.QuestionBanks.ById(bankId));
            return r.IsSuccess ? r.Data : null;
        }

        public Task<ApiResult<QuestionBankDto>> CreateBankAsync(QuestionBankVm vm)
            => _api.PostAsync<QuestionBankDto>(ApiRoutes.QuestionBanks.List, vm);

        public Task<ApiResult<QuestionBankDto>> UpdateBankAsync(int bankId, QuestionBankVm vm)
            => _api.PutAsync<QuestionBankDto>(ApiRoutes.QuestionBanks.ById(bankId), vm);

        public Task<ApiResult> DeleteBankAsync(int bankId)
            => _api.DeleteAsync(ApiRoutes.QuestionBanks.ById(bankId));

        // ---------- questions ----------
        public async Task<QuestionListVm> ListQuestionsAsync(int bankId, QuestionStatus? status, string? search, int page, int pageSize)
        {
            var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
            if (status.HasValue) qs.Add($"status={status.Value}");
            if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search.Trim())}");
            var r = await _api.GetAsync<PagedResultDto<AdminQuestionDto>>($"{ApiRoutes.QuestionBanks.Questions(bankId)}?{string.Join('&', qs)}");
            var data = r.Data ?? new PagedResultDto<AdminQuestionDto>();
            var vm = new QuestionListVm
            {
                Items = data.Items,
                Total = data.Total,
                Page = data.Page == 0 ? page : data.Page,
                PageSize = data.PageSize == 0 ? pageSize : data.PageSize,
                Status = status,
                Search = search
            };
            if (!r.IsSuccess) vm.SetError(r.StatusCode, r.DisplayMessage);
            return vm;
        }

        public async Task<AdminQuestionDto?> GetQuestionAsync(int questionId)
        {
            var r = await _api.GetAsync<AdminQuestionDto>(ApiRoutes.QuestionBanks.Question(questionId));
            return r.IsSuccess ? r.Data : null;
        }

        public Task<ApiResult> CreateQuestionAsync(QuestionEditVm vm)
        {
            var options = BuildOptions(vm);
            var payload = new[]
            {
                new
                {
                    vm.BankId,
                    vm.QuestionText,
                    vm.QuestionImageUrl,
                    QuestionType = vm.QuestionType,
                    DifficultyLevel = vm.DifficultyLevel,
                    vm.CorrectAnswer,
                    vm.Explanation,
                    Options = options
                }
            };
            return _api.PostAsync(ApiRoutes.QuestionBanks.CreateQuestions, payload);
        }

        public Task<ApiResult<AdminQuestionDto>> UpdateQuestionAsync(int questionId, QuestionEditVm vm)
            => _api.PutAsync<AdminQuestionDto>(ApiRoutes.QuestionBanks.Question(questionId), new
            {
                vm.QuestionText,
                vm.QuestionImageUrl,
                QuestionType = vm.QuestionType,
                DifficultyLevel = vm.DifficultyLevel,
                vm.CorrectAnswer,
                vm.Explanation,
                Options = BuildOptions(vm)
            });

        public Task<ApiResult> DeleteQuestionAsync(int questionId)
            => _api.DeleteAsync(ApiRoutes.QuestionBanks.Question(questionId));

        public Task<ApiResult> SubmitQuestionAsync(int questionId)
            => _api.PostAsync(ApiRoutes.QuestionBanks.SubmitQuestion(questionId));

        public Task<ApiResult> ReviewQuestionAsync(int questionId, bool approve, string? rejectReason)
            => _api.PostAsync(ApiRoutes.QuestionBanks.ReviewQuestion(questionId), new { Approve = approve, RejectReason = rejectReason });

        private static List<object> BuildOptions(QuestionEditVm vm)
        {
            var list = new List<object>();
            if (vm.QuestionType != QuestionType.MultipleChoice && vm.QuestionType != QuestionType.TrueFalse)
                return list;
            for (var i = 0; i < vm.OptionTexts.Count; i++)
            {
                var text = vm.OptionTexts[i]?.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                list.Add(new
                {
                    OptionText = text,
                    IsCorrect = vm.CorrectIndexes.Contains(i),
                    OrderIndex = list.Count
                });
            }
            return list;
        }
    }
}
