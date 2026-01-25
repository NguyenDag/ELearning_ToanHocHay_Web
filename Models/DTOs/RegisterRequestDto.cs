using System.ComponentModel.DataAnnotations;

namespace ToanHocHay.WebApp.Models.DTOs
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]

        public string FullName { get; set; }

        public string? Phone { get; set; }

        // UserType: 0 là Student, 1 là Parent
        public UserType UserType { get; set; }

        public int? GradeLevel { get; set; }

        public string? SchoolName { get; set; }

    }
}