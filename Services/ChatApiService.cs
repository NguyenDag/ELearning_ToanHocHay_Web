using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using ToanHocHay.WebApp.Common.Constants;

namespace ToanHocHay.WebApp.Services
{
    public class ChatApiService
    {
        private readonly HttpClient _httpClient;

        public ChatApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<JsonElement> SendMessageAsync(string userId, string text)
        {
            try
            {
                // Gọi tới ChatbotController của Backend C#
                var payload = new ChatMessagePayload { UserId = userId, Text = text };
                var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Chatbot.Message, payload);

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JsonElement>();
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse("{\"success\": false, \"response\": {\"message\": \"Không thể kết nối tới server backend.\"}}").RootElement;
            }
        }

        public async Task<JsonElement> SendQuickReplyAsync(string userId, string reply)
        {
            try
            {
                var payload = new ChatReplyPayload { UserId = userId, Reply = reply };
                var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Chatbot.QuickReply, payload);

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JsonElement>();
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse("{\"response\": {\"message\": \"Lỗi kết nối backend.\"}}").RootElement;
            }
        }

        public async Task<JsonElement> SendTriggerAsync(string userId, string trigger)
        {
            try
            {
                var payload = new ChatTriggerPayload { UserId = userId, Trigger = trigger };
                var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Chatbot.Trigger, payload);

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result;
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse("{\"response\": {\"message\": \"Hệ thống đang bận hoặc không thể kết nối tới máy chủ. Vui lòng quay lại sau.\"}}").RootElement;
            }
        }
    }

    // Payload classes với JsonPropertyName để kiểm soát JSON output
    public class ChatMessagePayload
    {
        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class ChatReplyPayload
    {
        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        [JsonPropertyName("reply")]
        public string Reply { get; set; }
    }

    public class ChatTriggerPayload
    {
        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        [JsonPropertyName("trigger")]
        public string Trigger { get; set; }
    }
}