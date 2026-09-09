using ToanHocHay.WebApp.Models.DTOs.Payment;

namespace ToanHocHay.WebApp.Areas.Admin.Models
{
    public static class PaymentEnums
    {
        public static string StatusLabel(PaymentStatusVm s) => s switch
        {
            PaymentStatusVm.Pending => "Chờ xử lý",
            PaymentStatusVm.Completed => "Hoàn tất",
            PaymentStatusVm.Failed => "Thất bại",
            PaymentStatusVm.Refunded => "Đã hoàn tiền",
            PaymentStatusVm.PartiallyRefunded => "Hoàn một phần",
            _ => s.ToString()
        };

        public static string StatusBadge(PaymentStatusVm s) => s switch
        {
            PaymentStatusVm.Completed => "bg-green-100 text-green-700",
            PaymentStatusVm.Pending => "bg-amber-100 text-amber-700",
            PaymentStatusVm.Failed => "bg-red-100 text-red-700",
            PaymentStatusVm.Refunded => "bg-gray-200 text-gray-600",
            PaymentStatusVm.PartiallyRefunded => "bg-orange-100 text-orange-700",
            _ => "bg-gray-100 text-gray-600"
        };

        public static string MethodLabel(PaymentMethodVm m) => m switch
        {
            PaymentMethodVm.CreditCard => "Thẻ tín dụng",
            PaymentMethodVm.BankTransfer => "Chuyển khoản",
            PaymentMethodVm.Momo => "MoMo",
            PaymentMethodVm.ZaloPay => "ZaloPay",
            PaymentMethodVm.VNPay => "VNPay",
            _ => m.ToString()
        };
    }

    // ---- DTO mirror của backend (Models/DTOs/Finance) ----

    public class RevenueSummaryDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int PayingCustomers { get; set; }
        public int NewSubscriptions { get; set; }
        public decimal Arpu { get; set; }
        public List<StatusSliceDto> StatusBreakdown { get; set; } = new();
        public List<MethodSliceDto> MethodBreakdown { get; set; } = new();
    }

    public class StatusSliceDto
    {
        public PaymentStatusVm Status { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class MethodSliceDto
    {
        public PaymentMethodVm Method { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class RevenueBucketDto
    {
        public DateTime PeriodStart { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public int TransactionCount { get; set; }
    }

    public class PackageRevenueDto
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = "";
        public PackageTierVm Tier { get; set; }
        public int TransactionCount { get; set; }
        public decimal GrossRevenue { get; set; }
    }

    public class TransactionRowDto
    {
        public int PaymentId { get; set; }
        public int StudentId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethodVm PaymentMethod { get; set; }
        public PaymentStatusVm Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? TransactionId { get; set; }
        public string? StudentName { get; set; }
        public string? PayerName { get; set; }
        public string? PackageName { get; set; }
        public PackageTierVm? PackageTier { get; set; }
    }

    // ---- View-models ----

    public class RevenueDashboardVm : AdminListVmBase
    {
        public RevenueSummaryDto Summary { get; set; } = new();
        public List<RevenueBucketDto> Series { get; set; } = new();
        public List<PackageRevenueDto> ByPackage { get; set; } = new();

        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Interval { get; set; } = "day";
    }

    public class TransactionFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Status { get; set; }
        public string? Method { get; set; }
        public string? Q { get; set; }
    }

    public class TransactionListVm : AdminListVmBase
    {
        public TransactionFilter Filter { get; set; } = new();
        public List<TransactionRowDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)) : 1;
    }
}
