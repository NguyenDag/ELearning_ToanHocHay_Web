namespace ToanHocHay.WebApp.Areas.Admin.Models
{
    /// <summary>
    /// Kết quả nạp một danh sách quản trị: dữ liệu đã phân trang + trạng thái lời gọi API
    /// (để controller/​view hiển thị lỗi thay vì báo "không có dữ liệu" gây hiểu nhầm).
    /// </summary>
    public class AdminList<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public int Total { get; init; }
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)) : 1;
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;

        /// <summary>null = thành công; ngược lại là thông báo lỗi (tiếng Việt) + mã HTTP.</summary>
        public string? Error { get; init; }
        public int ErrorStatus { get; init; }
        public bool Failed => Error != null;
        public bool Unauthorized => ErrorStatus == 401;

        public static AdminList<T> Ok(IEnumerable<T> items, int page, int pageSize, int total) => new()
        {
            Items = items.ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total
        };

        public static AdminList<T> Fail(string error, int status) => new()
        {
            Error = error,
            ErrorStatus = status
        };

        /// <summary>Phân trang phía client cho danh sách backend trả về đầy đủ.</summary>
        public static AdminList<T> Slice(IReadOnlyList<T> all, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var items = all.Skip((page - 1) * pageSize).Take(pageSize);
            return Ok(items, page, pageSize, all.Count);
        }
    }

    public static class AdminPaging
    {
        public const int DefaultPageSize = 20;

        public static int NormalizePage(int page) => page < 1 ? 1 : page;
        public static int NormalizeSize(int size) => size is < 1 or > 100 ? DefaultPageSize : size;
    }

    /// <summary>Cơ sở cho mọi view-model danh sách quản trị: mang thông tin lỗi nạp dữ liệu.</summary>
    public abstract class AdminListVmBase
    {
        public string? Error { get; set; }
        public int ErrorStatus { get; set; }
        public bool Failed => Error != null;
        public bool Unauthorized => ErrorStatus == 401;

        public void SetError(int status, string message)
        {
            ErrorStatus = status;
            Error = message;
        }
    }
}
