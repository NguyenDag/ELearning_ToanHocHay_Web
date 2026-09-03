namespace ToanHocHay.WebApp.Models.DTOs
{
    /// <summary>Trạng thái sinh nhận xét AI nền cho một lượt làm bài (P3).</summary>
    public class FeedbackStatusDto
    {
        public int TotalWrong { get; set; }
        public int Ready { get; set; }
        public int Pending { get; set; }
        public bool IsComplete { get; set; }
    }

    /// <summary>Hạn mức gợi ý AI trong ngày theo gói (P6).</summary>
    public class AiHintQuotaDto
    {
        public int Used { get; set; }
        public int Limit { get; set; }
        public bool Unlimited { get; set; }
        public int Remaining { get; set; }
    }
}
