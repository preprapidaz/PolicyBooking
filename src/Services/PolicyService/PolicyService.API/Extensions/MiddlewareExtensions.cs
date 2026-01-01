using PolicyService.API.Middleware;

namespace PolicyService.API.Extensions
{
    // <summary>
    /// Extension methods for middleware registration
    /// Makes Program.cs clean and readable
    /// </summary>
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingMiddleware>();
        }

        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
