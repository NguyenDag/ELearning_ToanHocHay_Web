using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services.Admin
{
    public class ExerciseAdminApiService
    {
        private readonly ApiClient _api;

        public ExerciseAdminApiService(ApiClient api) => _api = api;

        public async Task<List<ExerciseAdminDto>> ListAsync()
        {
            var r = await _api.GetAsync<List<ExerciseAdminDto>>(ApiRoutes.ExercisesAdmin.List);
            return r.IsSuccess && r.Data != null ? r.Data : new();
        }

        public async Task<ExerciseAdminDto?> GetAsync(int id)
        {
            var r = await _api.GetAsync<ExerciseAdminDto>(ApiRoutes.ExercisesAdmin.ById(id));
            return r.IsSuccess ? r.Data : null;
        }

        public async Task<List<ExerciseQuestionRowDto>> GetQuestionsAsync(int id)
        {
            var r = await _api.GetAsync<List<ExerciseQuestionRowDto>>(ApiRoutes.ExercisesAdmin.Questions(id));
            return r.IsSuccess && r.Data != null ? r.Data : new();
        }

        public Task<ApiResult<ExerciseAdminDto>> CreateAsync(ExerciseEditVm vm)
            => _api.PostAsync<ExerciseAdminDto>(ApiRoutes.ExercisesAdmin.List, Payload(vm));

        public Task<ApiResult<ExerciseAdminDto>> UpdateAsync(int id, ExerciseEditVm vm)
            => _api.PutAsync<ExerciseAdminDto>(ApiRoutes.ExercisesAdmin.ById(id), Payload(vm));

        public Task<ApiResult> DeleteAsync(int id)
            => _api.DeleteAsync(ApiRoutes.ExercisesAdmin.ById(id));

        public Task<ApiResult> PublishAsync(int id) => _api.PostAsync(ApiRoutes.ExercisesAdmin.Publish(id));
        public Task<ApiResult> UnpublishAsync(int id) => _api.PostAsync(ApiRoutes.ExercisesAdmin.Unpublish(id));

        public Task<ApiResult> AddQuestionsAsync(int id, IEnumerable<int> questionIds, double? scorePerQuestion)
            => _api.PostAsync(ApiRoutes.ExercisesAdmin.Questions(id),
                new { QuestionIds = questionIds.ToList(), ScorePerQuestion = scorePerQuestion });

        public Task<ApiResult> RemoveQuestionAsync(int exerciseId, int questionId)
            => _api.DeleteAsync(ApiRoutes.ExercisesAdmin.Question(exerciseId, questionId));

        public Task<ApiResult> SetScoreAsync(int exerciseId, int questionId, double score)
            => _api.PutAsync(ApiRoutes.ExercisesAdmin.Question(exerciseId, questionId), score);

        private static object Payload(ExerciseEditVm vm) => new
        {
            vm.TopicId,
            vm.ChapterId,
            vm.ExerciseName,
            ExerciseType = vm.ExerciseType,
            vm.TotalQuestions,
            vm.DurationMinutes,
            vm.IsFree,
            vm.IsActive,
            vm.TotalScores,
            vm.PassingScore,
            Status = vm.Status
        };
    }
}
