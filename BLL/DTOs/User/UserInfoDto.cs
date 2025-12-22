namespace DynamicForm.BLL.DTOs.User
{
    public class UserInfoDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? Department { get; set; } // ✅ NEW
        public string? DepartmentAr { get; set; } // ✅ NEW - Arabic name
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}