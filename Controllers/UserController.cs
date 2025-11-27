using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;
using DynamicForm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Tags("User Management")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        ///     Login with username and password
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _userService.LoginAsync(loginDto);

                if (result == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "اسم المستخدم أو كلمة المرور غير صحيحة"
                    });
                }

                return Ok(new ApiResponse<AuthResponseDto>
                {
                    Success = true,
                    Message = "تم تسجيل الدخول بنجاح",
                    Data = result
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Register a new employee (SuperAdmin only)
        /// </summary>
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserInfoDto>>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "البيانات المدخلة غير صحيحة",
                        Errors = ModelState
                    });
                }

                // Force role to Employee (only SuperAdmin can create accounts)
                registerDto.Role = UserRoles.Employee;

                var result = await _userService.RegisterAsync(registerDto);

                if (result == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "اسم المستخدم أو البريد الإلكتروني موجود بالفعل"
                    });
                }

                return CreatedAtAction(nameof(GetUser), new { id = result.UserId },
                    new ApiResponse<UserInfoDto>
                    {
                        Success = true,
                        Message = "تم تسجيل المستخدم بنجاح",
                        Data = result
                    });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var result = await _userService.RefreshTokenAsync(refreshTokenDto);

                if (result == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "رمز التحديث غير صالح أو منتهي الصلاحية"
                    });
                }

                return Ok(new ApiResponse<AuthResponseDto>
                {
                    Success = true,
                    Message = "تم تحديث الرمز بنجاح",
                    Data = result
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Logout and revoke refresh token
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> Logout()
        {
            try
            {
                var username = User.Identity?.Name;

                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "المستخدم غير معروف"
                    });
                }

                await _userService.RevokeTokenAsync(username);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "تم تسجيل الخروج بنجاح"
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Get all employees with pagination (SuperAdmin only)
        /// </summary>
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserInfoDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PagedResult<UserInfoDto>>>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                if (page < 1 || pageSize < 1 || pageSize > 50)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "معاملات الصفحة غير صحيحة"
                    });
                }

                var users = await _userService.GetAllUsersAsync(page, pageSize, isActive);

                return Ok(new ApiResponse<PagedResult<UserInfoDto>>
                {
                    Success = true,
                    Message = "تم جلب المستخدمين بنجاح",
                    Data = users
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Get employee by ID
        /// </summary>
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpGet("{id}", Name = "GetUser")]
        [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserInfoDto>>> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    });
                }

                return Ok(new ApiResponse<UserInfoDto>
                {
                    Success = true,
                    Message = "تم جلب المستخدم بنجاح",
                    Data = user
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Mark employee as active/inactive (SuperAdmin only)
        /// </summary>
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpPatch("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> UpdateUserStatus(
            int id,
            [FromBody] UpdateUserStatusDto statusDto)
        {
            try
            {
                var result = await _userService.UpdateUserStatusAsync(id, statusDto.IsActive);

                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    });
                }

                var statusText = statusDto.IsActive ? "نشط" : "غير نشط";

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"تم تحديث حالة المستخدم إلى {statusText} بنجاح"
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Soft delete employee (SuperAdmin only)
        /// </summary>
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> SoftDeleteUser(int id)
        {
            try
            {
                var result = await _userService.SoftDeleteUserAsync(id);

                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "تم حذف المستخدم بنجاح"
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Change password for current user
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var username = User.Identity?.Name;

                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "المستخدم غير معروف"
                    });
                }

                var result = await _userService.ChangePasswordAsync(username, changePasswordDto);

                if (!result)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "كلمة المرور الحالية غير صحيحة"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "تم تغيير كلمة المرور بنجاح"
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Reset password for any user (SuperAdmin only)
        /// </summary>
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpPost("{id}/reset-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> ResetPassword(int id, [FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var result = await _userService.ResetPasswordAsync(id, resetPasswordDto.NewPassword);

                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "تم إعادة تعيين كلمة المرور بنجاح"
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }
    }
}