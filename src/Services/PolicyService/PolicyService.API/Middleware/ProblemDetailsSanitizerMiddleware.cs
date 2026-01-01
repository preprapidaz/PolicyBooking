using System.Text;
using System.Text.Json;

namespace PolicyService.API.Middleware
{
    /// <summary>
    /// Sanitizes Problem Details responses in production
    /// Removes sensitive information like stack traces and file paths
    /// </summary>
    public class ProblemDetailsSanitizerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProblemDetailsSanitizerMiddleware> _logger;

        public ProblemDetailsSanitizerMiddleware(
            RequestDelegate next,
            IWebHostEnvironment env,
            ILogger<ProblemDetailsSanitizerMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Capture original response body
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                // Only process Problem Details responses
                if (context.Response.StatusCode >= 400 &&
                    context.Response.ContentType?.Contains("application/problem+json") == true)
                {
                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                    var bodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                    context.Response.Body.Seek(0, SeekOrigin.Begin);

                    // Parse and sanitize if not in development
                    if (!_env.IsDevelopment())
                    {
                        var sanitized = SanitizeProblemDetails(bodyText, context);

                        // Write sanitized response
                        context.Response.Body = originalBodyStream;
                        context.Response.ContentLength = null;
                        await context.Response.WriteAsync(sanitized);
                        return;
                    }
                }

                // Copy response back to original stream
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private string SanitizeProblemDetails(string problemDetailsJson, HttpContext context)
        {
            try
            {
                var doc = JsonDocument.Parse(problemDetailsJson);
                var root = doc.RootElement;

                var sanitized = new Dictionary<string, object>();

                // Copy safe properties
                if (root.TryGetProperty("type", out var type))
                    sanitized["type"] = type.GetString();

                if (root.TryGetProperty("title", out var title))
                    sanitized["title"] = title.GetString();

                if (root.TryGetProperty("status", out var status))
                    sanitized["status"] = status.GetInt32();

                if (root.TryGetProperty("detail", out var detail))
                {
                    var detailText = detail.GetString();
                    // Truncate if too long
                    sanitized["detail"] = detailText?.Length > 200
                        ? detailText.Substring(0, 200) + "..."
                        : detailText;
                }

                if (root.TryGetProperty("instance", out var instance))
                    sanitized["instance"] = instance.GetString();

                // Copy domain-specific safe properties
                if (root.TryGetProperty("errorCode", out var errorCode))
                    sanitized["errorCode"] = errorCode.GetString();

                if (root.TryGetProperty("policyId", out var policyId))
                    sanitized["policyId"] = policyId.GetString();

                if (root.TryGetProperty("policyNumber", out var policyNumber))
                    sanitized["policyNumber"] = policyNumber.GetString();

                if (root.TryGetProperty("currentState", out var currentState))
                    sanitized["currentState"] = currentState.GetString();

                if (root.TryGetProperty("attemptedAction", out var attemptedAction))
                    sanitized["attemptedAction"] = attemptedAction.GetString();

                if (root.TryGetProperty("errors", out var errors))
                    sanitized["errors"] = JsonSerializer.Deserialize<object>(errors.GetRawText());

                // Add correlation ID
                var correlationId = context.Items["CorrelationId"]?.ToString()
                    ?? Guid.NewGuid().ToString();

                sanitized["correlationId"] = correlationId;
                sanitized["timestamp"] = DateTime.UtcNow.ToString("O");
                sanitized["traceId"] = context.TraceIdentifier;

                // Add production-safe metadata
                sanitized["support"] = "support@policyservice.com";
                sanitized["help"] = "https://docs.policyservice.com/errors";
                sanitized["contactSupport"] = $"Please contact support with correlationId: {correlationId}";

                // ❌ REMOVE these sensitive properties
                // - exceptionDetails
                // - stackFrames  
                // - raw
                // - stackTrace
                // - Any file paths

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return JsonSerializer.Serialize(sanitized, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing problem details");
                return problemDetailsJson; // Return original if sanitization fails
            }
        }
    }

    public static class ProblemDetailsSanitizerExtensions
    {
        public static IApplicationBuilder UseProblemDetailsSanitizer(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ProblemDetailsSanitizerMiddleware>();
        }
    }
}