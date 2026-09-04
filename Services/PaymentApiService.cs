using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs.Payment;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>Lịch sử thanh toán của người dùng (P5). <c>GET /api/payments/me</c> có phân trang.</summary>
    public class PaymentApiService
    {
        private readonly ApiClient _api;

        public PaymentApiService(ApiClient api) => _api = api;

        public Task<ApiResult<PagedResultDto<PaymentDto>>> GetMyPaymentsAsync(int page = 1, int pageSize = 20)
            => _api.GetAsync<PagedResultDto<PaymentDto>>($"{ApiRoutes.Payments.Mine}?page={page}&pageSize={pageSize}");

        public Task<ApiResult<PaymentDto>> GetPaymentAsync(int id)
            => _api.GetAsync<PaymentDto>(ApiRoutes.Payments.ById(id));
    }
}
