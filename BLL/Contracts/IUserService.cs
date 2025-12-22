using DynamicForm.BLL.DTOs.General;
using DynamicForm.BLL.DTOs.User;

namespace DynamicForm.BLL.Contracts
{
    public interface IUserService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<UserInfoDto?> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<bool> RevokeTokenAsync(string username);
        Task<UserInfoDto?> GetUserByIdAsync(int userId);
        Task<PagedResult<UserInfoDto>> GetAllUsersAsync(int page, int pageSize, bool? isActive = null);
        Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
        Task<bool> SoftDeleteUserAsync(int userId);
        Task<bool> ChangePasswordAsync(string username, ChangePasswordDto changePasswordDto);
        Task<bool> ResetPasswordAsync(int userId, string newPassword);
    }
}