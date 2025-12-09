using System.Security.Claims;
using HRPayrollSystem.API.Services;

namespace HRPayrollSystem.API.Middleware;

/// <summary>
/// 權限驗證中介軟體
/// 驗證使用者是否擁有存取資源的權限
/// </summary>
public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(
        RequestDelegate next,
        ILogger<AuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuthorizationService authorizationService)
    {
        // 如果是公開端點，直接通過
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            await _next(context);
            return;
        }

        // 檢查使用者是否已認證
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // 取得使用者 ID
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("無法從令牌中取得使用者 ID");
            await _next(context);
            return;
        }

        // 檢查是否需要特定權限
        var requiredPermission = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>();
        if (requiredPermission != null)
        {
            var hasPermission = await authorizationService.HasPermissionAsync(
                userId,
                requiredPermission.Permission
            );

            if (!hasPermission)
            {
                _logger.LogWarning(
                    "使用者 {UserId} 嘗試存取需要權限 {Permission} 的資源但被拒絕",
                    userId,
                    requiredPermission.Permission
                );

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "您沒有權限執行此操作"
                });
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// 允許匿名存取的屬性
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AllowAnonymousAttribute : Attribute
{
}

/// <summary>
/// 需要特定權限的屬性
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute
{
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// 權限驗證中介軟體擴充方法
/// </summary>
public static class AuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationMiddleware>();
    }
}
