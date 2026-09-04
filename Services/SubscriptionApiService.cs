using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Gói &amp; subscription (P5). Backend: giá lấy từ <c>Package.Price</c> (KHÔNG nhận
    /// <c>AmountPaid</c>); vòng đời tự động (Active hết hạn → Expired, Pending quá 30′ → Cancelled).
    /// </summary>
    public class SubscriptionApiService
    {
        private readonly ApiClient _api;

        public SubscriptionApiService(ApiClient api) => _api = api;

        /// <summary>POST /api/subscriptions — trả { subscriptionId, amount, qrUrl }.</summary>
        public async Task<CreateSubscriptionResultDto?> CreateSubscriptionAsync(int studentId, int packageId, decimal amount = 0)
        {
            var r = await _api.PostAsync<CreateSubscriptionResultDto>(
                ApiRoutes.Subscriptions.Create, new { StudentId = studentId, PackageId = packageId });
            return r.IsSuccess ? r.Data : null;
        }

        /// <summary>GET /api/students/{id}/subscription/current — dùng cho phụ huynh xem con.</summary>
        public async Task<SubscriptionInfoDto?> GetCurrentSubscriptionAsync(int studentId)
        {
            var r = await _api.GetAsync<SubscriptionInfoDto>(ApiRoutes.Students.CurrentSubscription(studentId));
            return r.IsSuccess ? r.Data : null;
        }

        /// <summary>GET /api/subscriptions/me — gói của chính người đăng nhập (Free khi chưa có).</summary>
        public async Task<SubscriptionInfoDto?> GetMySubscriptionAsync()
        {
            var r = await _api.GetAsync<SubscriptionInfoDto>(ApiRoutes.Subscriptions.Mine);
            return r.IsSuccess ? r.Data : null;
        }

        /// <summary>GET /api/subscriptions/status/{id} — { status, endDate } cho màn QR.</summary>
        public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(int subscriptionId)
        {
            var r = await _api.GetAsync<SubscriptionStatusDto>(ApiRoutes.Subscriptions.Status(subscriptionId));
            return r.IsSuccess ? r.Data : null;
        }

        /// <summary>PUT /api/subscriptions/cancel/{id}.</summary>
        public Task<ApiResult> CancelAsync(int subscriptionId)
            => _api.PutAsync(ApiRoutes.Subscriptions.Cancel(subscriptionId));
    }
}
