using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs.Notification;
using ToanHocHay.WebApp.Models.DTOs.Payment;   // PagedResultDto<T>
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>Thông báo trong ứng dụng (P6). Sinh theo luật: chuyển tab, điểm thấp, nghỉ học.</summary>
    public class NotificationApiService
    {
        private readonly ApiClient _api;

        public NotificationApiService(ApiClient api) => _api = api;

        public Task<ApiResult<PagedResultDto<NotificationDto>>> GetMineAsync(int page = 1, int pageSize = 20, bool unreadOnly = false)
            => _api.GetAsync<PagedResultDto<NotificationDto>>(
                $"{ApiRoutes.Notifications.List}?unreadOnly={unreadOnly.ToString().ToLowerInvariant()}&page={page}&pageSize={pageSize}");

        public Task<ApiResult<int>> GetUnreadCountAsync()
            => _api.GetAsync<int>(ApiRoutes.Notifications.UnreadCount);

        public Task<ApiResult> MarkReadAsync(int id)
            => _api.PostAsync(ApiRoutes.Notifications.Read(id));

        public Task<ApiResult> MarkAllReadAsync()
            => _api.PostAsync(ApiRoutes.Notifications.ReadAll);

        public Task<ApiResult<List<NotificationPreferenceDto>>> GetPreferencesAsync()
            => _api.GetAsync<List<NotificationPreferenceDto>>(ApiRoutes.Notifications.Preferences);

        public Task<ApiResult> SetPreferenceAsync(string ruleKey, bool enabled)
            => _api.PutAsync(ApiRoutes.Notifications.Preferences, new SetNotificationPreferenceDto { RuleKey = ruleKey, Enabled = enabled });
    }
}
