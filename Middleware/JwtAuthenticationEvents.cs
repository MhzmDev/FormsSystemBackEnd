using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace DynamicForm.Middleware
{
    public class JwtAuthenticationEvents : JwtBearerEvents
    {
        public override Task Challenge(JwtBearerChallengeContext context)
        {
            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false, message = GetArabicMessage(context), messageEn = GetEnglishMessage(context), error = "Unauthorized"
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        public override Task AuthenticationFailed(AuthenticationFailedContext context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var message = "فشلت عملية المصادقة";
            var messageEn = "Authentication failed";

            if (context.Exception is SecurityTokenExpiredException)
            {
                message = "انتهت صلاحية رمز الوصول. يرجى تحديث الرمز";
                messageEn = "Access token has expired. Please refresh your token";
            }
            else if (context.Exception is SecurityTokenInvalidSignatureException)
            {
                message = "توقيع الرمز غير صالح";
                messageEn = "Invalid token signature";
            }
            else if (context.Exception is SecurityTokenInvalidIssuerException)
            {
                message = "مصدر الرمز غير صالح";
                messageEn = "Invalid token issuer";
            }
            else if (context.Exception is SecurityTokenInvalidAudienceException)
            {
                message = "جمهور الرمز غير صالح";
                messageEn = "Invalid token audience";
            }

            var response = new { success = false, message, messageEn, error = context.Exception.GetType().Name };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private static string GetArabicMessage(JwtBearerChallengeContext context)
        {
            if (context.AuthenticateFailure != null)
            {
                if (context.AuthenticateFailure is SecurityTokenExpiredException)
                {
                    return "انتهت صلاحية رمز الوصول. يرجى تحديث الرمز";
                }

                return "رمز المصادقة غير صالح أو منتهي الصلاحية";
            }

            if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
            {
                return "رمز المصادقة مطلوب. يرجى تسجيل الدخول";
            }

            return "فشلت عملية المصادقة";
        }

        private static string GetEnglishMessage(JwtBearerChallengeContext context)
        {
            if (context.AuthenticateFailure != null)
            {
                if (context.AuthenticateFailure is SecurityTokenExpiredException)
                {
                    return "Access token has expired. Please refresh your token";
                }

                return "Invalid or expired authentication token";
            }

            if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
            {
                return "Authentication token is required. Please login";
            }

            return "Authentication failed";
        }
    }
}