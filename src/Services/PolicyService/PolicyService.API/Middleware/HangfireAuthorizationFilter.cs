using Hangfire.Dashboard;

namespace PolicyService.API.Middleware
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // DEMO: Allow all users to access dashboard
            // PRODUCTION: Check authentication
            return true;

            // Production example:
            // var httpContext = context.GetHttpContext();
            // return httpContext.User.Identity?.IsAuthenticated ?? false;

            // Or check for specific role:
            // return httpContext.User.IsInRole("Admin");
        }
    }
}