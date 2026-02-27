// Thêm vào file Models/DTOs/ trong WebApp
// Tạo file mới: PaymentDTOs.cs

namespace ToanHocHay.WebApp.Models.DTOs
{
    public class PackageDto
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public bool UnlimitedAiHint { get; set; }
        public bool PersonalizedPath { get; set; }
        public bool MistakeRetry { get; set; }
        public bool SmartReminder { get; set; }
        public bool PrioritySupport { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSubscriptionResultDto
    {
        public int SubscriptionId { get; set; }
        public string QrUrl { get; set; } = "";
    }

    public class SubscriptionStatusDto
    {
        public int SubscriptionId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending | Active | Expired | Cancelled
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}