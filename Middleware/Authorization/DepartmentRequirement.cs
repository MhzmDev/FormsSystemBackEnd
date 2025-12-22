using DynamicForm.DAL.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace DynamicForm.Middleware.Authorization;

public class DepartmentRequirement : IAuthorizationRequirement
{
    public DepartmentRequirement(params string[] allowedDepartments)
    {
        AllowedDepartments = allowedDepartments;
    }

    public string[] AllowedDepartments { get; }
}

public class DepartmentAuthorizationHandler : AuthorizationHandler<DepartmentRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentRequirement requirement)
    {
        // ✅ SuperAdmin and Admin bypass department checks
        var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (role == UserRoles.SuperAdmin || role == UserRoles.Admin)
        {
            context.Succeed(requirement);

            return Task.CompletedTask;
        }

        // ✅ Check if user's department matches allowed departments
        var userDepartment = context.User.FindFirst("Department")?.Value;

        if (userDepartment != null && requirement.AllowedDepartments.Contains(userDepartment))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}