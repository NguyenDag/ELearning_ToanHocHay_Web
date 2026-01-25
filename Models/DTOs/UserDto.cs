namespace ToanHocHay.WebApp.Models.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public UserType UserType { get; set; }
        public string? SchoolName { get; set; }
        public int? GradeLevel { get; set; }
    }
}