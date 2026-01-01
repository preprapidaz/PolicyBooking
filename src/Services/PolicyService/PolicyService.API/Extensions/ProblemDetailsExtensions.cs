using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PolicyService.Domain.Exceptions;

namespace PolicyService.API.Extensions
{
    public static class ProblemDetailsExtensions
    {
        public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
        {
            services.AddProblemDetails(options =>
            {
                // Version 6.x syntax
                options.IncludeExceptionDetails = (ctx, ex) =>
                {
                    var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
                    return env.IsDevelopment();
                };

                // Map to status codes
                options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);
                options.MapToStatusCode<HttpRequestException>(StatusCodes.Status503ServiceUnavailable);
                options.MapToStatusCode<ArgumentNullException>(StatusCodes.Status400BadRequest);
                options.MapToStatusCode<UnauthorizedAccessException>(StatusCodes.Status401Unauthorized);

                // Map PolicyNotFoundException
                options.Map<PolicyNotFoundException>(ex =>
                {
                    return new ProblemDetails
                    {
                        Type = "https://policyservice.com/errors/policy-not-found",
                        Title = "Policy Not Found",
                        Status = StatusCodes.Status404NotFound,
                        Detail = ex.Message,
                        Extensions =
                        {
                            ["errorCode"] = ex.ErrorCode,
                            ["policyId"] = ex.PolicyId.ToString()
                        }
                    };
                });

                // Map DuplicatePolicyException
                options.Map<DuplicatePolicyException>(ex =>
                {
                    return new ProblemDetails
                    {
                        Type = "https://policyservice.com/errors/duplicate-policy",
                        Title = "Duplicate Policy",
                        Status = StatusCodes.Status409Conflict,
                        Detail = ex.Message,
                        Extensions =
                        {
                            ["errorCode"] = ex.ErrorCode,
                            ["policyNumber"] = ex.PolicyNumber
                        }
                    };
                });

                // Map PolicyValidationException
                options.Map<PolicyValidationException>(ex =>
                {
                    return new ValidationProblemDetails(ex.ValidationErrors)
                    {
                        Type = "https://policyservice.com/errors/validation-failed",
                        Title = "Validation Failed",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = ex.Message
                    };
                });

                // Map InvalidPolicyStateException
                options.Map<InvalidPolicyStateException>(ex =>
                {
                    return new ProblemDetails
                    {
                        Type = "https://policyservice.com/errors/invalid-state",
                        Title = "Invalid Policy State",
                        Status = StatusCodes.Status409Conflict,
                        Detail = ex.Message,
                        Extensions =
                        {
                            ["errorCode"] = ex.ErrorCode,
                            ["currentState"] = ex.CurrentState,
                            ["attemptedAction"] = ex.AttemptedAction
                        }
                    };
                });

                // Map PremiumCalculationException
                options.Map<PremiumCalculationException>(ex =>
                {
                    return new ProblemDetails
                    {
                        Type = "https://policyservice.com/errors/premium-calculation",
                        Title = "Premium Calculation Failed",
                        Status = StatusCodes.Status422UnprocessableEntity,
                        Detail = ex.Message,
                        Extensions =
                        {
                            ["errorCode"] = ex.ErrorCode
                        }
                    };
                });

                // Map ArgumentException
                options.Map<ArgumentException>(ex =>
                {
                    return new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        Title = "Bad Request",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = ex.Message
                    };
                });

                // Map InvalidOperationException
                options.Map<InvalidOperationException>(ex =>
                {
                    return new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        Title = "Invalid Operation",
                        Status = StatusCodes.Status409Conflict,
                        Detail = ex.Message
                    };
                });

                // Map KeyNotFoundException
                options.Map<KeyNotFoundException>(ex =>
                {
                    return new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                        Title = "Not Found",
                        Status = StatusCodes.Status404NotFound,
                        Detail = ex.Message
                    };
                });
            });

            return services;
        }
    }

    /// <summary>
    /// Middleware to enrich Problem Details responses
    /// </summary>
    public class ProblemDetailsEnricherMiddleware
    {
        private readonly RequestDelegate _next;

        public ProblemDetailsEnricherMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // If response is a problem details response
            if (context.Response.StatusCode >= 400 &&
                context.Response.ContentType?.Contains("application/problem+json") == true)
            {
                var correlationId = context.Items["CorrelationId"]?.ToString()
                    ?? Guid.NewGuid().ToString();

                context.Response.Headers["X-Correlation-Id"] = correlationId;
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            }
        }
    }

    /// <summary>
    /// Extension to add enricher middleware
    /// </summary>
    public static class ProblemDetailsEnricherExtensions
    {
        public static IApplicationBuilder UseProblemDetailsEnricher(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ProblemDetailsEnricherMiddleware>();
        }
    }
}