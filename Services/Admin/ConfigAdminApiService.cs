using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services.Admin
{
    public class ConfigAdminApiService
    {
        private readonly ApiClient _api;

        public ConfigAdminApiService(ApiClient api) => _api = api;

        public async Task<List<SystemConfigItemDto>> GetAllAsync()
        {
            var r = await _api.GetAsync<List<SystemConfigItemDto>>(ApiRoutes.Admin.Config);
            return r.IsSuccess && r.Data != null ? r.Data : new List<SystemConfigItemDto>();
        }

        public Task<ApiResult> SetAsync(string key, string? value)
            => _api.PutAsync(ApiRoutes.Admin.ConfigKey(key), new { Value = value });
    }
}
