using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services.Admin
{
    /// <summary>Quản lý gói &amp; giá (Finance/Admin). Tier cố định — chỉ sửa giá / tính năng / bật-tắt.</summary>
    public class PackageAdminApiService
    {
        private readonly ApiClient _api;

        public PackageAdminApiService(ApiClient api) => _api = api;

        public async Task<PackageListVm> ListAsync()
        {
            var r = await _api.GetAsync<List<PackageAdminDto>>(ApiRoutes.Packages.Manage);
            var vm = new PackageListVm
            {
                Items = (r.Data ?? new()).OrderBy(p => p.Tier).ThenBy(p => p.Price).ToList()
            };
            if (!r.IsSuccess) vm.SetError(r.StatusCode, r.DisplayMessage);
            return vm;
        }

        public async Task<PackageAdminDto?> GetAsync(int id)
        {
            var r = await _api.GetAsync<PackageAdminDto>(ApiRoutes.Packages.ById(id));
            return r.IsSuccess ? r.Data : null;
        }

        public Task<ApiResult> UpdateAsync(int id, PackageEditVm vm)
            => _api.PutAsync(ApiRoutes.Packages.ById(id), Payload(vm));

        public async Task<ApiResult> SetActiveAsync(int id, bool isActive)
        {
            var current = await GetAsync(id);
            if (current == null) return ApiResult.From(ApiResult<object>.Fail(404, "Không tìm thấy gói"));

            return await _api.PutAsync(ApiRoutes.Packages.ById(id), Payload(new PackageEditVm
            {
                PackageName = current.PackageName,
                Description = current.Description,
                Price = current.Price,
                DurationDays = current.DurationDays,
                AiHintLimitDaily = current.AiHintLimitDaily,
                UnlimitedAiHint = current.UnlimitedAiHint,
                PersonalizedPath = current.PersonalizedPath,
                MistakeRetry = current.MistakeRetry,
                SmartReminder = current.SmartReminder,
                PrioritySupport = current.PrioritySupport,
                IsActive = isActive
            }));
        }

        private static object Payload(PackageEditVm vm) => new
        {
            vm.PackageName,
            vm.Description,
            vm.Price,
            vm.DurationDays,
            vm.AiHintLimitDaily,
            vm.UnlimitedAiHint,
            vm.PersonalizedPath,
            vm.MistakeRetry,
            vm.SmartReminder,
            vm.PrioritySupport,
            vm.IsActive
        };
    }
}
