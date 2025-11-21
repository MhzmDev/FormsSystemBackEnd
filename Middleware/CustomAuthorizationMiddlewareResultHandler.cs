using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Text.Json;

namespace DynamicForm.Middleware
{
    public class CustomAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            // If authorization succeeded, continue normally
            if (authorizeResult.Succeeded)
            {
                await next(context);

                return;
            }

            // If authorization failed, handle the response
            if (authorizeResult.Challenged)
            {
                // User is not authenticated
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    message = "يجب تسجيل الدخول للوصول إلى هذا المورد",
                    messageEn = "You must be logged in to access this resource",
                    error = "Unauthorized"
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));

                return;
            }

            if (authorizeResult.Forbidden)
            {
                // User is authenticated but lacks required role/permission
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var requiredRole = GetRequiredRole(policy);
                var userRole = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

                var message = string.IsNullOrEmpty(requiredRole)
                    ? "ليس لديك صلاحية للوصول إلى هذا المورد"
                    : $"يتطلب هذا المورد دور '{requiredRole}'. دورك الحالي: '{userRole ?? "غير محدد"}'";

                var messageEn = string.IsNullOrEmpty(requiredRole)
                    ? "You do not have permission to access this resource"
                    : $"This resource requires '{requiredRole}' role. Your current role: '{userRole ?? "None"}'";

                var response = new
                {
                    success = false,
                    message,
                    messageEn,
                    requiredRole,
                    currentRole = userRole,
                    error = "Forbidden"
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));

                return;
            }

            // Fallback to default handler
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }

        private static string GetRequiredRole(AuthorizationPolicy policy)
        {
            var roleRequirement = policy.Requirements.OfType<RolesAuthorizationRequirement>().FirstOrDefault();

            return roleRequirement?.AllowedRoles.FirstOrDefault() ?? string.Empty;
        }
    }
}