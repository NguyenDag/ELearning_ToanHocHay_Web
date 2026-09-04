using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs.Payment;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Hoàn tiền — workflow bán tự động (P8). Endpoint 1-bước cũ
    /// <c>POST /api/payments/{id}/refund</c> đã bị XOÁ.
    /// Người dùng: tạo yêu cầu → chờ Finance duyệt → chi tiền tay → xác nhận.
    /// </summary>
    public class RefundApiService
    {
        private readonly ApiClient _api;

        public RefundApiService(ApiClient api) => _api = api;

        /// <summary>POST /api/refunds — 400 điều kiện, 409 đã có yêu cầu / hết hạn mức, 429 quá nhanh.</summary>
        public Task<ApiResult<RefundRequestDto>> CreateAsync(CreateRefundRequestDto dto)
            => _api.PostAsync<RefundRequestDto>(ApiRoutes.Refunds.Create, dto);

        public Task<ApiResult<PagedResultDto<RefundRequestDto>>> GetMineAsync(int page = 1, int pageSize = 20)
            => _api.GetAsync<PagedResultDto<RefundRequestDto>>($"{ApiRoutes.Refunds.Mine}?page={page}&pageSize={pageSize}");

        public Task<ApiResult<RefundRequestDetailDto>> GetAsync(int id)
            => _api.GetAsync<RefundRequestDetailDto>(ApiRoutes.Refunds.ById(id));
    }
}
