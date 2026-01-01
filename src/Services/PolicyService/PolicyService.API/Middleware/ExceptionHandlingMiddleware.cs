using System.Net;
using System.Text.Json;

namespace PolicyService.API.Middleware
{
    // <summary>
    /// Global exception handler middleware
    /// Follows SRP: Only responsible for exception handling
    /// Returns RFC 7807 Problem Details for consistency
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
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
            // Get correlation ID from request
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

            // Log exception with correlation ID
            _logger.LogError(exception,
                "An error occurred. CorrelationId: {CorrelationId}, Path: {Path}",
                correlationId,
                context.Request.Path);

            // Determine status code based on exception type
            var (statusCode, title) = exception switch
            {
                ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found"),
                InvalidOperationException => (HttpStatusCode.Conflict, "Conflict"),
                _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
            };

            // Create RFC 7807 Problem Details response
            var problemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = title,
                status = (int)statusCode,
                detail = _env.IsDevelopment() ? exception.Message : "An error occurred processing your request.",
                instance = context.Request.Path.Value,
                correlationId = correlationId,
                timestamp = DateTime.UtcNow,
                // Only include stack trace in development
                stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
            };

            // Set response
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            await context.Response.WriteAsJsonAsync(problemDetails, options);
        }
    }
}
