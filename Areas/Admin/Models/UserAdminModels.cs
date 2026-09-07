using System.ComponentModel.DataAnnotations;
using ToanHocHay.WebApp.Models.DTOs;

namespace ToanHocHay.WebApp.Areas.Admin.Models
{
    /// <summary>Người dùng như backend trả về cho khu quản trị (khớp <c>UserDto</c> của backend).</summary>
    public class AdminUserDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Phone { get; set; }
        public UserType UserType { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public DateTime? LockedAt { get; set; }
        public string? LockedReason { get; set; }
        public DateTime? LockoutEndsAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsLocked => LockedAt.HasValue;
    }

    /// <summary>Bộ lọc danh sách người dùng (map thẳng lên query string backend).</summary>
    public class AdminUserFilter
    {
        public string? Search { get; set; }
        public UserType? UserType { get; set; }
        public bool? IsActive { get; set; }
        public bool? Locked { get; set; }
        public bool? EmailConfirmed { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CreateStaffUserVm
    {
        [Required(ErrorMessage = "Nhập họ tên"), MaxLength(255)]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ"), MaxLength(255)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Nhập mật khẩu"), MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự"), MaxLength(100)]
        public string Password { get; set; } = "";

        [Required]
        public UserType UserType { get; set; } = ToanHocHay.WebApp.Models.DTOs.UserType.ContentEditor;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }
    }

    public class RoleCapabilityDto
    {
        public UserType Role { get; set; }
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public bool CanManageUsers { get; set; }
        public bool CanAuthorContent { get; set; }
        public bool CanReviewContent { get; set; }
        public bool CanPublishContent { get; set; }
        public bool CanManageFinance { get; set; }
        public bool CanManageConfig { get; set; }
        public bool CanViewAuditLog { get; set; }
    }

    public class UsersIndexVm : AdminListVmBase
    {
        public AdminUserFilter Filter { get; set; } = new();
        public List<AdminUserDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)) : 1;
    }

    public static class AdminRoles
    {
        /// <summary>Vai trò nhân sự — admin tạo được trực tiếp.</summary>
        public static readonly UserType[] Staff =
        {
            UserType.ContentEditor, UserType.AcademicReviewer, UserType.SupportStaff,
            UserType.FinanceManager, UserType.SystemAdmin
        };

        public static string Label(UserType t) => t switch
        {
            UserType.Student => "Học sinh",
            UserType.Parent => "Phụ huynh",
            UserType.ContentEditor => "Biên tập nội dung",
            UserType.AcademicReviewer => "Thẩm định học thuật",
            UserType.SupportStaff => "Nhân viên hỗ trợ",
            UserType.FinanceManager => "Quản lý tài chính",
            UserType.SystemAdmin => "Quản trị hệ thống",
            _ => t.ToString()
        };
    }
}
