using Azure.Messaging.ServiceBus.Administration;

namespace PolicyService.API.Jobs
{
    /// <summary>
    /// Monitors dead letter queue and sends alerts if threshold exceeded
    /// Runs every 15 minutes
    /// </summary>
    public class DeadLetterQueueMonitorJob
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeadLetterQueueMonitorJob> _logger;

        public DeadLetterQueueMonitorJob(
            IConfiguration configuration,
            ILogger<DeadLetterQueueMonitorJob> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("[Hangfire] Starting DLQ monitoring job...");

            try
            {
                var connectionString = _configuration["AzureServiceBusConnectionString"];
                var queueName = _configuration["ServiceBus:QueueName"] ?? "policy-processing-queue";

                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("Service Bus connection string not configured");
                    return;
                }

                // Create Service Bus admin client
                var adminClient = new ServiceBusAdministrationClient(connectionString);

                // Get queue runtime properties
                var queueProperties = await adminClient.GetQueueRuntimePropertiesAsync(queueName);

                var dlqCount = queueProperties.Value.DeadLetterMessageCount;
                var activeCount = queueProperties.Value.ActiveMessageCount;
                var totalMessageCount = queueProperties.Value.TotalMessageCount;

                _logger.LogInformation(
                    "Queue Status | Queue: {Queue} | Active: {Active} | DLQ: {DLQ} | Total: {Total}",
                    queueName,
                    activeCount,
                    dlqCount,
                    totalMessageCount);

                // Alert if DLQ threshold exceeded
                var alertThreshold = _configuration.GetValue<int>("Hangfire:DLQAlertThreshold", 10);

                if (dlqCount > alertThreshold)
                {
                    _logger.LogWarning(
                        "DLQ ALERT | {Count} messages in dead letter queue (threshold: {Threshold})",
                        dlqCount,
                        alertThreshold);

                    // In production: Send notification
                    // await _notificationService.SendAlertAsync(
                    //     "Dead Letter Queue Alert",
                    //     $"Queue '{queueName}' has {dlqCount} messages in DLQ. Threshold: {alertThreshold}");
                }
                else
                {
                    _logger.LogInformation(
                        "DLQ count within acceptable range | Count: {Count} | Threshold: {Threshold}",
                        dlqCount,
                        alertThreshold);
                }

                // Store metrics (for monitoring dashboard)
                // await _metricsService.RecordDLQMetricsAsync(queueName, dlqCount, activeCount);

                _logger.LogInformation("[Hangfire] DLQ monitoring completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] DLQ monitoring failed");
                throw; // Let Hangfire handle retry
            }
        }
    }
}