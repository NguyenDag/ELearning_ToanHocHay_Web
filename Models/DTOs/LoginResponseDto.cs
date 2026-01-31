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
        public string AvatarUrl { get; set; } = null!;
    }
}