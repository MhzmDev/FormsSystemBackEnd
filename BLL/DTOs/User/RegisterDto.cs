using System.ComponentModel.DataAnnotations;

namespace DynamicForm.BLL.DTOs.User
{
    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? Department { get; set; }
    }
}