using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs.Chatbot;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Chatbot (P6). Backend nay lưu hội thoại phía C#, lấy UserId TỪ TOKEN (không nhận từ body),
    /// class-level <c>[Authorize]</c> — chỉ <c>/health</c> ẩn danh.
    /// <c>POST message</c> body = <c>{ text }</c>, trả <see cref="ChatTurnResultDto"/>.
    /// </summary>
    public class ChatApiService
    {
        private readonly ApiClient _api;

        public ChatApiService(ApiClient api) => _api = api;

        public Task<ApiResult<ChatTurnResultDto>> SendMessageAsync(string text)
            => _api.PostAsync<ChatTurnResultDto>(ApiRoutes.Chatbot.Message, new { text });

        public Task<ApiResult<ChatTurnResultDto>> SendQuickReplyAsync(string text)
            => _api.PostAsync<ChatTurnResultDto>(ApiRoutes.Chatbot.QuickReply, new { text });

        /// <summary>Trigger chủ động (scroll, thời gian...). Trả object loose của AI.</summary>
        public Task<ApiResult<object>> SendTriggerAsync(string trigger)
            => _api.PostAsync<object>(ApiRoutes.Chatbot.Trigger, new { trigger });

        public Task<ApiResult<ChatTurnResultDto>> RequestHumanAsync()
            => _api.PostAsync<ChatTurnResultDto>(ApiRoutes.Chatbot.RequestHuman);

        public Task<ApiResult<List<ChatConversationVm>>> GetConversationsAsync()
            => _api.GetAsync<List<ChatConversationVm>>(ApiRoutes.Chatbot.Conversations);

        public Task<ApiResult<List<ChatMessageVm>>> GetMessagesAsync(int conversationId)
            => _api.GetAsync<List<ChatMessageVm>>(ApiRoutes.Chatbot.ConversationMessages(conversationId));

        /// <summary>GET /api/chatbot/health — 200 khi AI sẵn sàng, 503 khi down.</summary>
        public async Task<bool> IsHealthyAsync()
        {
            var r = await _api.GetAsync<HealthDto>(ApiRoutes.Chatbot.Health);
            return r.StatusCode == 200;
        }

        private sealed class HealthDto { public string? Status { get; set; } }
    }
}
