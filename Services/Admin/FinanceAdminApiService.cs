using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs.Payment;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services.Admin
{
    /// <summary>Tổng hợp doanh thu + danh sách giao dịch cho khu tài chính.</summary>
    public class FinanceAdminApiService
    {
        private readonly ApiClient _api;

        public FinanceAdminApiService(ApiClient api) => _api = api;

        private static string Range(DateTime? from, DateTime? to, params string[] extra)
        {
            var qs = new List<string>(extra);
            if (from.HasValue) qs.Add($"from={from.Value:yyyy-MM-dd}");
            if (to.HasValue) qs.Add($"to={to.Value:yyyy-MM-dd}");
            return qs.Count > 0 ? "?" + string.Join('&', qs) : "";
        }

        public async Task<RevenueDashboardVm> GetDashboardAsync(DateTime? from, DateTime? to, string interval)
        {
            var vm = new RevenueDashboardVm { Interval = interval };

            var summary = await _api.GetAsync<RevenueSummaryDto>(ApiRoutes.Finance.AnalyticsSummary + Range(from, to));
            var series = await _api.GetAsync<List<RevenueBucketDto>>(
                ApiRoutes.Finance.RevenueSeries + Range(from, to, $"interval={interval}"));
            var byPkg = await _api.GetAsync<List<PackageRevenueDto>>(ApiRoutes.Finance.RevenueByPackage + Range(from, to));

            if (!summary.IsSuccess) { vm.SetError(summary.StatusCode, summary.DisplayMessage); return vm; }

            vm.Summary = summary.Data ?? new();
            vm.Series = series.Data ?? new();
            vm.ByPackage = byPkg.Data ?? new();
            vm.From = vm.Summary.From;
            vm.To = vm.Summary.To;
            return vm;
        }

        public async Task<TransactionListVm> GetTransactionsAsync(TransactionFilter f)
        {
            var qs = new List<string> { $"page={f.Page}", $"pageSize={f.PageSize}" };
            if (f.From.HasValue) qs.Add($"from={f.From.Value:yyyy-MM-dd}");
            if (f.To.HasValue) qs.Add($"to={f.To.Value:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(f.Status)) qs.Add($"status={f.Status}");
            if (!string.IsNullOrWhiteSpace(f.Method)) qs.Add($"method={f.Method}");
            if (!string.IsNullOrWhiteSpace(f.Q)) qs.Add($"search={Uri.EscapeDataString(f.Q.Trim())}");

            var r = await _api.GetAsync<PagedResultDto<TransactionRowDto>>(
                $"{ApiRoutes.Payments.All}?{string.Join('&', qs)}");

            var data = r.Data ?? new PagedResultDto<TransactionRowDto>();
            var vm = new TransactionListVm
            {
                Filter = f,
                Items = data.Items,
                Total = data.Total,
                Page = data.Page == 0 ? f.Page : data.Page,
                PageSize = data.PageSize == 0 ? f.PageSize : data.PageSize
            };
            if (!r.IsSuccess) vm.SetError(r.StatusCode, r.DisplayMessage);
            return vm;
        }
    }
}
