namespace ToanHocHay.WebApp.Common.Constants
{
    /// <summary>
    /// URL gốc của Backend API và của chính WebApp.
    /// Giá trị được nạp từ cấu hình lúc khởi động (<c>Program.cs</c>):
    /// <c>Api:BaseUrl</c> / <c>Api:WebBaseUrl</c> trong <c>appsettings.json</c>, hoặc biến
    /// môi trường <c>Api__BaseUrl</c> / <c>Api__WebBaseUrl</c> (file <c>.env</c>).
    /// Các giá trị gán sẵn dưới đây chỉ là mặc định dự phòng khi thiếu cấu hình.
    /// </summary>
    public static class ApiConstant
    {
        /// <summary>URL gốc Backend API (không kèm <c>/api</c>).</summary>
        public static string apiBaseUrl = "http://103.98.152.182";

        /// <summary>URL công khai của WebApp (dùng cho liên kết tuyệt đối, email...).</summary>
        public static string webBaseUrl = "https://www.toanhochay.com";
    }
}
