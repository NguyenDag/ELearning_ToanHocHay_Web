using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToanHocHay.WebApp.Services.Http
{
    /// <summary>
    /// Cấu hình JSON dùng chung cho MỌI lời gọi tới Backend API.
    /// Backend đã bật <c>JsonStringEnumConverter</c> → enum được serialize thành chuỗi
    /// ("Student", "Premium"...). Nếu thiếu converter này, việc deserialize enum sẽ ném exception.
    /// </summary>
    public static class ApiJson
    {
        public static readonly JsonSerializerOptions Options = Build();

        private static JsonSerializerOptions Build()
        {
            var o = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                // Gửi đi giữ nguyên PascalCase để khớp DTO backend (PropertyNamingPolicy = null).
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            o.Converters.Add(new JsonStringEnumConverter());
            return o;
        }
    }
}
