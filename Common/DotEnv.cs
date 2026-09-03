namespace ToanHocHay.WebApp.Common
{
    /// <summary>
    /// Nạp file <c>.env</c> vào biến môi trường của tiến trình TRƯỚC khi
    /// <see cref="WebApplication.CreateBuilder"/> chạy, để cấu hình trong đó ghi đè
    /// <c>appsettings.json</c> (theo cơ chế <c>AddEnvironmentVariables</c> mặc định của .NET).
    ///
    /// Quy ước: dùng <c>__</c> (hai gạch dưới) làm dấu phân cấp, ví dụ
    /// <c>Api__BaseUrl</c> ↔ <c>Api:BaseUrl</c>.
    /// Biến môi trường thật (đã set sẵn ở hệ điều hành / container) luôn được ưu tiên,
    /// file <c>.env</c> chỉ điền vào chỗ còn trống.
    /// </summary>
    public static class DotEnv
    {
        public static void Load(string path)
        {
            if (!File.Exists(path)) return;

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                var idx = line.IndexOf('=');
                if (idx <= 0) continue;

                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();

                // Bỏ dấu nháy bao ngoài nếu có.
                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value[1..^1];
                }

                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
