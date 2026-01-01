using System.Diagnostics;
using System.Text;

namespace PolicyService.API.Middleware
{
    /// <summary>
    /// Middleware to log all HTTP requests and responses
    /// Follows SRP: Only responsible for request/response logging
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            // Generate correlation ID for request tracking
            var correlationId = Guid.NewGuid().ToString();
            context.Items["CorrelationId"] = correlationId;

            // Start timer
            var stopwatch = Stopwatch.StartNew();

            // Log incoming request
            await LogRequestAsync(context, correlationId);

            // Capture original response body stream
            var originalBodyStream = context.Response.Body;

            try
            {
                // Create new memory stream for response body
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // Call next middleware in pipeline
                await _next(context);

                // Stop timer
                stopwatch.Stop();

                // Log outgoing response
                await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);

                // Copy response back to original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log exception
                _logger.LogError(ex,
                    "Unhandled exception occurred. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                    correlationId,
                    stopwatch.ElapsedMilliseconds);

                // Restore original body stream
                context.Response.Body = originalBodyStream;

                throw; // Re-throw to let exception middleware handle it
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogRequestAsync(HttpContext context, string correlationId)
        {
            // Enable buffering to read body multiple times
            context.Request.EnableBuffering();

            // Read request body
            var requestBody = await ReadRequestBodyAsync(context.Request);

            _logger.LogInformation(
                "HTTP Request | CorrelationId: {CorrelationId} | Method: {Method} | Path: {Path} | QueryString: {QueryString} | Body: {Body}",
                correlationId,
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                requestBody);

            // Reset stream position for next middleware
            context.Request.Body.Position = 0;
        }

        private async Task LogResponseAsync(HttpContext context, string correlationId, long duration)
        {
            // Read response body
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            var logLevel = context.Response.StatusCode >= 400
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(logLevel,
                "HTTP Response | CorrelationId: {CorrelationId} | StatusCode: {StatusCode} | Duration: {Duration}ms | Body: {Body}",
                correlationId,
                context.Response.StatusCode,
                duration,
                responseBody);
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            // Skip body reading for GET requests
            if (request.Method == HttpMethods.Get)
                return string.Empty;

            try
            {
                using var reader = new StreamReader(
                    request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);

                var body = await reader.ReadToEndAsync();

                // Reset stream position
                request.Body.Position = 0;

                return body;
            }
            catch
            {
                return "[Unable to read request body]";
            }
        }
    }
}
