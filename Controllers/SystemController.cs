using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;

namespace DynamicForm.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("System Metadata")]
public class SystemController : ControllerBase
{
    /// <summary>
    ///     Get all available roles
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Roles = UserRoles.SuperAdmin)] // Only SuperAdmin can see roles
    [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<RoleDto>>> GetRoles()
    {
        var roles = new List<RoleDto>
        {
            new() { Name = UserRoles.SuperAdmin, DisplayName = "مدير النظام" },
            new() { Name = UserRoles.Admin, DisplayName = "مدير" },
            new() { Name = UserRoles.Employee, DisplayName = "موظف" }
        };

        return Ok(new ApiResponse<List<RoleDto>>
        {
            Success = true,
            Message = "تم جلب الأدوار بنجاح",
            Data = roles
        });
    }

    /// <summary>
    ///     Get all available departments
    /// </summary>
    [HttpGet("departments")]
    [Authorize(Policy = "AdminOrAbove")] // Admin and SuperAdmin can see departments
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<DepartmentDto>>> GetDepartments()
    {
        var departments = new List<DepartmentDto>
        {
            new() { Name = Departments.Sales, NameAr = Departments.SalesAr },
            new() { Name = Departments.Marketing, NameAr = Departments.MarketingAr }
        };

        return Ok(new ApiResponse<List<DepartmentDto>>
        {
            Success = true,
            Message = "تم جلب الأقسام بنجاح",
            Data = departments
        });
    }
}