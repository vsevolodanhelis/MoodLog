using System.Net;
using System.Text.Json;

namespace MoodLog.Web.Middleware;

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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case ArgumentException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = exception.Message;
                errorResponse.Type = "Validation Error";
                break;

            case InvalidOperationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = exception.Message;
                errorResponse.Type = "Operation Error";
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = "You don't have permission to access this resource.";
                errorResponse.Type = "Authorization Error";
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = "The requested resource was not found.";
                errorResponse.Type = "Not Found";
                break;

            case TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Message = "The request timed out. Please try again.";
                errorResponse.Type = "Timeout Error";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "An unexpected error occurred. Please try again later.";
                errorResponse.Type = "Internal Server Error";
                break;
        }

        // For AJAX requests, return JSON
        if (IsAjaxRequest(context.Request))
        {
            var jsonResponse = JsonSerializer.Serialize(new
            {
                success = false,
                message = errorResponse.Message,
                type = errorResponse.Type
            });

            await response.WriteAsync(jsonResponse);
        }
        else
        {
            // For regular requests, redirect to error page
            var errorPageUrl = response.StatusCode switch
            {
                404 => "/Error/NotFound",
                401 => "/Error/Unauthorized",
                _ => "/Error/Index"
            };

            context.Response.Redirect(errorPageUrl);
        }
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return request.Headers.ContainsKey("X-Requested-With") &&
               request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
