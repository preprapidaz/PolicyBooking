using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace PolicyService.Worker.Resilience
{
    public static class PollyPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        logger.LogWarning(
                            "Retry {RetryAttempt} after {Delay}s",
                            retryAttempt,
                            timespan.TotalSeconds);
                    });
        }

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
                            "CIRCUIT BREAKER OPENED | Will retry after {Duration}s",
                            duration.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("CIRCUIT BREAKER CLOSED");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("CIRCUIT BREAKER HALF-OPEN");
                    });
        }

        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(10),
                timeoutStrategy: TimeoutStrategy.Pessimistic);
        }
    }
}