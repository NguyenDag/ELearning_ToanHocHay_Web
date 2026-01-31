using System.Text;
using System.Text.Json;

namespace ToanHocHay.WebApp.Services
{
    public class ChatApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _chatbotUrl;

        public ChatApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Lấy URL và kiểm tra xem nó có tồn tại không
            _chatbotUrl = configuration["AI:ChatbotApiUrl"] ?? "";

            if (string.IsNullOrEmpty(_chatbotUrl))
            {
                // In ra console để bạn dễ debug khi chạy ứng dụng
                Console.WriteLine("CRITICAL: AI:ChatbotApiUrl is missing in appsettings.json");
            }
        }

        public async Task<JsonElement> SendMessageAsync(string userId, string text)
        {
            try
            {
                var payload = new { user_id = userId, text = text };
                var response = await _httpClient.PostAsJsonAsync($"{_chatbotUrl}/message", payload);

                response.EnsureSuccessStatusCode(); // Ném lỗi nếu server trả về 4xx hoặc 5xx
                return await response.Content.ReadFromJsonAsync<JsonElement>();
            }
            catch (Exception ex)
            {
                // Trả về một đối tượng lỗi giả lập để Frontend xử lý thay vì crash app
                return JsonDocument.Parse("{\"success\": false, \"response\": {\"message\": \"Không thể kết nối tới server AI.\"}}").RootElement;
            }
        }

        public async Task<JsonElement> SendQuickReplyAsync(string userId, string reply)
        {
            try
            {
                var payload = new { user_id = userId, reply = reply };

                // Làm sạch URL: Đảm bảo không bị thừa dấu "/"
                string baseUrl = _chatbotUrl.TrimEnd('/');
                string requestUrl = $"{baseUrl}/quick-reply";

                // Log ra cửa sổ Output của Visual Studio để bạn kiểm tra URL thực tế
                Console.WriteLine($"[DEBUG] Calling AI API: {requestUrl}");

                var response = await _httpClient.PostAsJsonAsync(requestUrl, payload);

                // Nếu Python báo lỗi (500), ta lấy nội dung lỗi đó ra xem
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ERROR] Python Logic Error: {errorContent}");
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JsonElement>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] Connection Failed: {ex.Message}");
                return JsonDocument.Parse("{\"response\": {\"message\": \"Lỗi kết nối AI. Kiểm tra Terminal Python ngay!\"}}").RootElement;
            }
        }
    }
}