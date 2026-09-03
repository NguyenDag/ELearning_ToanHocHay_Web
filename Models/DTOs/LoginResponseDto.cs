using System;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public enum UserType
    {
        Student,           // 0
        Parent,            // 1
        ContentEditor,     // 2
        AcademicReviewer,  // 3
        SupportStaff,      // 4
        FinanceManager,    // 5
        SystemAdmin        // 6
    }

    /// <summary>Bậc gói — khớp enum PackageTier của backend (serialize dạng chuỗi).</summary>
    public enum PackageTier
    {
        Free,
        Standard,
        Premium,
        Yearly
    }

    public class LoginResponseDto
    {
        public int UserId { get; set; }
        public int? StudentId { get; set; }
        public int? ParentId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public UserType UserType { get; set; }

        public string Token { get; set; } = null!;
        public DateTime TokenExpiration { get; set; }

        // Backend rút access token còn 30 phút — FE phải dùng refresh token.
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiration { get; set; }

        public string AvatarUrl { get; set; } = null!;

        /// <summary>Bậc gói hiện tại (thay cho PackageType int cũ).</summary>
        public PackageTier PackageTier { get; set; } = PackageTier.Free;
    }
}
