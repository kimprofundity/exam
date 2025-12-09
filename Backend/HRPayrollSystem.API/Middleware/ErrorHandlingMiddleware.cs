using System.Net;
using System.Text.Json;

namespace HRPayrollSystem.API.Middleware;

/// <summary>
/// 全域錯誤處理中介軟體
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "發生內部伺服器錯誤";
        var errorCode = "INTERNAL_ERROR";

        // 根據異常類型設定不同的回應
        switch (exception)
        {
            case InvalidOperationException when exception.Message.Contains("LDAP"):
                statusCode = HttpStatusCode.ServiceUnavailable;
                message = "無法連接到身份驗證伺服器，請稍後再試";
                errorCode = "LDAP_CONNECTION_ERROR";
                _logger.LogError(exception, "LDAP 連線失敗");
                break;

            case InvalidOperationException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                errorCode = "INVALID_OPERATION";
                _logger.LogWarning(exception, "無效的操作");
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "未授權的存取";
                errorCode = "UNAUTHORIZED";
                _logger.LogWarning(exception, "未授權的存取嘗試");
                break;

            case ArgumentException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                errorCode = "INVALID_ARGUMENT";
                _logger.LogWarning(exception, "無效的參數");
                break;

            default:
                _logger.LogError(exception, "未處理的異常");
                break;
        }

        var response = new
        {
            success = false,
            errorCode,
            message,
            timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// 錯誤處理中介軟體擴充方法
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
