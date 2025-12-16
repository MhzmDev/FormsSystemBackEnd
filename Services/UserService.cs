using DynamicForm.Data;
using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace DynamicForm.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, IJwtService jwtService, ILogger<UserService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username && !u.IsDeleted);

            if (user == null || !VerifyPasswordHash(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", loginDto.Username);

                return null;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {Username}", loginDto.Username);

                return null;
            }

            // Update last login date
            user.LastLoginDate = DateTime.UtcNow;

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = _jwtService.GetRefreshTokenExpiry();

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} logged in successfully", user.Username);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = _jwtService.GetAccessTokenExpiry(),
                RefreshTokenExpiry = user.RefreshTokenExpiryTime.Value,
                User = MapToUserInfoDto(user)
            };
        }

        public async Task<UserInfoDto?> RegisterAsync(RegisterDto registerDto)
        {
            // ✅ Validate role
            if (!UserRoles.IsValid(registerDto.Role))
            {
                throw new ArgumentException($"الدور غير صحيح. الأدوار المتاحة: {string.Join(", ", UserRoles.All)}");
            }

            // ✅ Validate department for Employees
            if (registerDto.Role == UserRoles.Employee)
            {
                if (string.IsNullOrEmpty(registerDto.Department))
                {
                    throw new ArgumentException("القسم مطلوب للموظفين");
                }

                if (!Departments.IsValid(registerDto.Department))
                {
                    throw new ArgumentException($"القسم غير صحيح. الأقسام المتاحة: {string.Join(", ", Departments.All)}");
                }
            }
            else
            {
                // ✅ SuperAdmin and Admin don't need department
                registerDto.Department = null;
            }

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                _logger.LogWarning("Registration failed: Username {Username} already exists", registerDto.Username);

                return null;
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", registerDto.Email);

                return null;
            }

            // Prevent creating SuperAdmin via registration
            if (registerDto.Role == UserRoles.SuperAdmin)
            {
                registerDto.Role = UserRoles.Employee;
                _logger.LogWarning("Attempted to create SuperAdmin via registration, role forced to Employee");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                FullName = registerDto.FullName,
                PhoneNumber = registerDto.PhoneNumber,
                Role = registerDto.Role,
                Department = registerDto.Department,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} registered successfully with role {Role}", user.Username, user.Role);

            return MapToUserInfoDto(user);
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var principal = _jwtService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);

            if (principal == null)
            {
                _logger.LogWarning("Invalid access token provided for refresh");

                return null;
            }

            var username = principal.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Username not found in token claims");

                return null;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User {Username} not found or inactive during token refresh", username);

                return null;
            }

            if (user.RefreshToken != refreshTokenDto.RefreshToken)
            {
                _logger.LogWarning("Invalid refresh token for user {Username}", username);

                return null;
            }

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired for user {Username}", username);

                return null;
            }

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = _jwtService.GetRefreshTokenExpiry();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tokens refreshed for user {Username}", username);

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = _jwtService.GetAccessTokenExpiry(),
                RefreshTokenExpiry = user.RefreshTokenExpiryTime.Value,
                User = MapToUserInfoDto(user)
            };
        }

        public async Task<bool> RevokeTokenAsync(string username)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);

            if (user == null)
            {
                return false;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Token revoked for user {Username}", username);

            return true;
        }

        public async Task<UserInfoDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            return user == null ? null : MapToUserInfoDto(user);
        }

        public async Task<PagedResult<UserInfoDto>> GetAllUsersAsync(int page, int pageSize, bool? isActive = null)
        {
            var query = _context.Users
                .Where(u => !u.IsDeleted)
                .AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .OrderByDescending(u => u.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<UserInfoDto>
            {
                Items = users.Select(MapToUserInfoDto),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (user == null)
            {
                return false;
            }

            // Prevent deactivating SuperAdmin
            if (user.Role == UserRoles.SuperAdmin && !isActive)
            {
                _logger.LogWarning("Attempted to deactivate SuperAdmin user {Username}", user.Username);

                return false;
            }

            user.IsActive = isActive;
            user.ModifiedDate = DateTime.UtcNow;

            // Revoke tokens if user is being deactivated
            if (!isActive)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} status updated to {IsActive}", user.Username, isActive);

            return true;
        }

        public async Task<bool> SoftDeleteUserAsync(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (user == null)
            {
                return false;
            }

            // Prevent deleting SuperAdmin
            if (user.Role == UserRoles.SuperAdmin)
            {
                _logger.LogWarning("Attempted to delete SuperAdmin user {Username}", user.Username);

                return false;
            }

            user.IsDeleted = true;
            user.IsActive = false;
            user.ModifiedDate = DateTime.UtcNow;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} soft deleted", user.Username);

            return true;
        }

        public async Task<bool> ChangePasswordAsync(string username, ChangePasswordDto changePasswordDto)
        {
            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
            {
                _logger.LogWarning("Password confirmation does not match for user {Username}", username);

                return false;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);

            if (user == null || !VerifyPasswordHash(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Failed password change attempt for user {Username}", username);

                return false;
            }

            user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
            user.ModifiedDate = DateTime.UtcNow;

            // Revoke all refresh tokens to force re-login
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for user {Username}", username);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (user == null)
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            user.ModifiedDate = DateTime.UtcNow;

            // Revoke all refresh tokens to force re-login
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset successfully for user {Username}", user.Username);

            return true;
        }

        // Helper methods
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            var hashOfInput = HashPassword(password);

            return hashOfInput == storedHash;
        }

        private UserInfoDto MapToUserInfoDto(User user)
        {
            return new UserInfoDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                Department = user.Department, // ✅ NEW
                DepartmentAr = user.Department != null ? Departments.GetArabicName(user.Department) : null, // ✅ NEW
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate
            };
        }
    }
}