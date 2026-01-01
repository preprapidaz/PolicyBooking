using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace PolicyService.API.Resilience
{
    public static class PollyPolicies
    {
        /// <summary>
        /// Retry policy: 3 attempts with exponential backoff
        /// Wait times: 2s, 4s, 8s
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // 5xx, 408, or network failures
                .Or<TimeoutRejectedException>() // Timeout exceptions
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        logger.LogWarning(
                            "Retry {RetryAttempt} after {Delay}s | Reason: {Reason}",
                            retryAttempt,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });
        }

        /// <summary>
        /// Circuit breaker policy: Open circuit after 5 consecutive failures
        /// Stay open for 30 seconds, then try again
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, duration) =>
                    {
                        logger.LogError(
                            "CIRCUIT BREAKER OPENED | Will retry after {Duration}s | Reason: {Reason}",
                            duration.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("CIRCUIT BREAKER CLOSED | Service recovered");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("CIRCUIT BREAKER HALF-OPEN | Testing if service recovered");
                    });
        }

        /// <summary>
        /// Timeout policy: Don't wait more than 10 seconds
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(10),
                timeoutStrategy: TimeoutStrategy.Pessimistic);
        }

        /// <summary>
        /// Bulkhead policy: Limit concurrent requests to 10
        /// Prevents one slow endpoint from consuming all threads
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetBulkheadPolicy(ILogger logger)
        {
            return Policy.BulkheadAsync<HttpResponseMessage>(
                maxParallelization: 10,
                maxQueuingActions: 20,
                onBulkheadRejectedAsync: context =>
                {
                    logger.LogWarning("BULKHEAD LIMIT REACHED | Request rejected");
                    return Task.CompletedTask;
                });
        }

        /// <summary>
        /// Fallback policy: Return default response if all else fails
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(ILogger logger)
        {
            return Policy<HttpResponseMessage>
                .Handle<Exception>()
                .FallbackAsync(
                    fallbackAction: async (context, cancellationToken) =>
                    {
                        logger.LogWarning("FALLBACK TRIGGERED | Returning service unavailable");

                        var response = new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                        {
                            Content = new StringContent("{\"message\":\"Service temporarily unavailable\"}")
                        };

                        return await Task.FromResult(response);
                    },
                    onFallbackAsync: (outcome, context) =>
                    {
                        logger.LogError(
                            "Fallback executed | Reason: {Reason}",
                            outcome.Exception?.Message ?? "Unknown");
                        return Task.CompletedTask;
                    });
        }
    }
}