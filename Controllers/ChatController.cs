using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Services;
using System.Security.Claims;
using System.Text.Json.Serialization; // Cần thiết để mapping tên trường

namespace ToanHocHay.WebApp.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatApiService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(ChatApiService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] ChatMessageRequest request)
        {
            try
            {
                // 1. Lấy UserId từ hệ thống (Identity/Session)
                string userId = GetUserId();
                _logger.LogInformation($"[ChatController.Send] UserId: {userId}, Message: {request?.Text}");
                
                // 2. Gửi sang Service. 
                // Đảm bảo trong _chatService.SendMessageAsync, dữ liệu được gửi đi với key là "UserId"
                var result = await _chatService.SendMessageAsync(userId, request.Text);
                _logger.LogInformation($"[ChatController.Send] Backend Response: {System.Text.Json.JsonSerializer.Serialize(result)}");
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChatController.Send] Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickReply([FromBody] QuickReplyRequest request)
        {
            try
            {
                string userId = GetUserId();
                _logger.LogInformation($"[ChatController.QuickReply] UserId: {userId}, Reply: {request?.Reply}");
                
                var result = await _chatService.SendQuickReplyAsync(userId, request.Reply);
                _logger.LogInformation($"[ChatController.QuickReply] Backend Response: {System.Text.Json.JsonSerializer.Serialize(result)}");
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ChatController.QuickReply] Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private string GetUserId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // Lấy ID từ Database người dùng đã đăng nhập
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "guest";
                _logger.LogDebug($"[ChatController.GetUserId] Authenticated user: {userId}");
                return userId;
            }
            // Nếu là khách, dùng ID phiên làm việc tạm thời
            var sessionId = HttpContext.Session.Id ?? "anonymous";
            _logger.LogDebug($"[ChatController.GetUserId] Anonymous user: {sessionId}");
            return sessionId;
        }
    }

    // --- Sửa lại các Class Request để khớp chuẩn Database UserId ---

    public class ChatMessageRequest 
    { 
        [JsonPropertyName("UserId")] // Khớp với Database
        public string? UserId { get; set; } 

        [JsonPropertyName("text")]   // Khớp với Python API (thường dùng chữ thường)
        public string Text { get; set; } = string.Empty;
    }

    public class QuickReplyRequest 
    { 
        [JsonPropertyName("UserId")] // Khớp với Database
        public string? UserId { get; set; }

        [JsonPropertyName("reply")]  // Khớp với Python API
        public string Reply { get; set; } = string.Empty;
    }
}