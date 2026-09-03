namespace ToanHocHay.WebApp.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>Correlation-id để đối chiếu với log của WebApp / Backend khi báo lỗi.</summary>
    public string? CorrelationId { get; set; }
    public bool ShowCorrelationId => !string.IsNullOrEmpty(CorrelationId);

    public int? StatusCode { get; set; }

    /// <summary>Thông điệp thân thiện hiển thị cho người dùng.</summary>
    public string Message { get; set; } = "Đã có lỗi xảy ra khi xử lý yêu cầu của bạn.";
}
