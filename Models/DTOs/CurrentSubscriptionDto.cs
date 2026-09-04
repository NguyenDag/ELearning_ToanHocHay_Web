namespace ToanHocHay.WebApp.Models.DTOs
{
    public class CurrentSubscriptionDto
    {
        public int SubscriptionId { get; set; }
        public int PackageId { get; set; }
        public string PackageName { get; set; } = "";
        public PackageTier PackageTier { get; set; } = PackageTier.Free;
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}