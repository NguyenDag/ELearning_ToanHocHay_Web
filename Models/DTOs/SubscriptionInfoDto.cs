namespace ToanHocHay.WebApp.Models.DTOs
{
    public class SubscriptionInfoDto
    {
        public PackageTier PackageTier { get; set; } = PackageTier.Free;
        public string PackageName { get; set; } = "Free";
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = false;
        public int DaysRemaining { get; set; } = 0;
        public bool UnlimitedAiHint { get; set; } = false;
        public int? AiHintLimitDaily { get; set; } = 0;
        public bool PersonalizedPath { get; set; } = false;
        public bool MistakeRetry { get; set; } = false;
        public bool SmartReminder { get; set; } = false;
        public bool PrioritySupport { get; set; } = false;
    }
}