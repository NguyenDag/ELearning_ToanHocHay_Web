using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Services.Admin;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    /// <summary>Doanh thu (biểu đồ + KPI) &amp; danh sách giao dịch — SystemAdmin + FinanceManager.</summary>
    public class FinanceController : FinanceBaseController
    {
        private readonly FinanceAdminApiService _finance;

        public FinanceController(FinanceAdminApiService finance) => _finance = finance;

        [HttpGet]
        public async Task<IActionResult> Revenue(DateTime? from, DateTime? to, string interval = "day")
        {
            interval = interval?.ToLowerInvariant() switch
            {
                "week" => "week",
                "month" => "month",
                _ => "day"
            };

            var vm = await _finance.GetDashboardAsync(from, to, interval);
            if (GuardListError(vm) is { } redirect) return redirect;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Transactions(
            int page = 1, DateTime? from = null, DateTime? to = null,
            string? status = null, string? method = null, string? q = null)
        {
            var filter = new TransactionFilter
            {
                Page = page < 1 ? 1 : page,
                PageSize = 20,
                From = from,
                To = to,
                Status = status,
                Method = method,
                Q = q
            };

            var vm = await _finance.GetTransactionsAsync(filter);
            if (GuardListError(vm) is { } redirect) return redirect;
            return View(vm);
        }
    }
}
