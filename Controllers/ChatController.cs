using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.DTOs.Chatbot;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatApiService _chat;
        private readonly ILogger<ChatController> _logger;

        public ChatController(ChatApiService chat, ILogger<ChatController> logger)
        {
            _chat = chat;
            _logger = logger;
        }

        private bool IsAuthed => User.Identity?.IsAuthenticated == true;

        // Chuẩn hoá về shape mà wwwroot/js/chatbot.js đang hiểu:
        //   { success, response: { message, options }, conversationId, aiAvailable, status, needLogin }
        private IActionResult Turn(ApiResult<ChatTurnResultDto> r)
        {
            if (r.IsUnauthorized)
                return Json(new { success = false, needLogin = true, response = new { message = "Vui lòng đăng nhập để trò chuyện với trợ lý." } });

            if (!r.IsSuccess || r.Data == null)
                return Json(new { success = false, response = new { message = r.DisplayMessage } });

            var d = r.Data;
            return Json(new
            {
                success = true,
                conversationId = d.ConversationId,
                aiAvailable = d.AiAvailable,
                status = d.ConversationStatus.ToString(),
                response = new
                {
                    message = d.Reply?.Body ?? "",
                    options = d.Options
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] ChatMessageRequest request)
        {
            if (!IsAuthed) return Json(new { success = false, needLogin = true, response = new { message = "Vui lòng đăng nhập để trò chuyện với trợ lý." } });
            return Turn(await _chat.SendMessageAsync(request?.Text ?? ""));
        }

        [HttpPost]
        public async Task<IActionResult> QuickReply([FromBody] QuickReplyRequest request)
        {
            if (!IsAuthed) return Json(new { success = false, needLogin = true, response = new { message = "Vui lòng đăng nhập." } });
            return Turn(await _chat.SendQuickReplyAsync(request?.Reply ?? ""));
        }

        [HttpPost]
        public async Task<IActionResult> Trigger([FromBody] TriggerRequest request)
        {
            // Trigger cần đăng nhập ở backend — với khách thì bỏ qua êm.
            if (!IsAuthed) return Json(new { success = true, response = (object?)null });

            var r = await _chat.SendTriggerAsync(request?.TriggerType ?? "");
            return Json(r.IsSuccess ? r.Data ?? new { } : new { success = false });
        }

        // Gặp nhân viên hỗ trợ
        [HttpPost]
        public async Task<IActionResult> RequestHuman()
        {
            if (!IsAuthed) return Json(new { success = false, needLogin = true, response = new { message = "Vui lòng đăng nhập." } });
            return Turn(await _chat.RequestHumanAsync());
        }

        // Trạng thái dịch vụ AI (widget disable input khi down)
        [HttpGet]
        public async Task<IActionResult> Health()
            => Json(new { healthy = await _chat.IsHealthyAsync() });

        // Lịch sử hội thoại gần nhất (nạp khi mở widget)
        [HttpGet]
        public async Task<IActionResult> History()
        {
            if (!IsAuthed) return Json(new { messages = Array.Empty<object>() });

            var convs = await _chat.GetConversationsAsync();
            var latest = convs.IsSuccess ? convs.Data?.OrderByDescending(c => c.CreatedAt).FirstOrDefault() : null;
            if (latest == null) return Json(new { messages = Array.Empty<object>() });

            var msgs = await _chat.GetMessagesAsync(latest.ConversationId);
            var list = (msgs.IsSuccess ? msgs.Data : null) ?? new List<ChatMessageVm>();
            return Json(new
            {
                conversationId = latest.ConversationId,
                status = latest.Status.ToString(),
                messages = list.OrderBy(m => m.SentAt).Select(m => new
                {
                    from = m.SenderType == ChatSender.User ? "user" : "bot",
                    text = m.Body,
                    sentAt = m.SentAt
                })
            });
        }
    }

    public class ChatMessageRequest { public string Text { get; set; } = ""; }
    public class QuickReplyRequest { public string Reply { get; set; } = ""; }
    public class TriggerRequest
    {
        [JsonPropertyName("trigger")] public string TriggerType { get; set; } = "";
    }
}
