using Azure.Messaging.ServiceBus;

namespace PolicyService.API.Jobs
{
    /// <summary>
    /// Reprocesses messages from dead letter queue
    /// Triggered manually from Hangfire dashboard
    /// </summary>
    public class DeadLetterQueueReprocessJob
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeadLetterQueueReprocessJob> _logger;

        public DeadLetterQueueReprocessJob(
            IConfiguration configuration,
            ILogger<DeadLetterQueueReprocessJob> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ExecuteAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? errorReasonFilter = null,
            int maxMessages = 100)
        {
            _logger.LogInformation(
                "[Hangfire] Starting DLQ reprocessing | FromDate: {From} | ToDate: {To} | ErrorFilter: {Filter} | MaxMessages: {Max}",
                fromDate?.ToString("yyyy-MM-dd") ?? "All",
                toDate?.ToString("yyyy-MM-dd") ?? "All",
                errorReasonFilter ?? "All",
                maxMessages);

            var connectionString = _configuration["AzureServiceBusConnectionString"];
            var queueName = _configuration["ServiceBus:QueueName"] ?? "policy-processing-queue";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Service Bus connection string not configured");
                return;
            }

            await using var client = new ServiceBusClient(connectionString);

            // Create receiver for dead letter queue
            await using var dlqReceiver = client.CreateReceiver(
                queueName,
                new ServiceBusReceiverOptions
                {
                    SubQueue = SubQueue.DeadLetter,
                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                });

            // Create sender for main queue
            await using var sender = client.CreateSender(queueName);

            var processedCount = 0;
            var skippedCount = 0;
            var failedCount = 0;

            try
            {
                while (processedCount < maxMessages)
                {
                    // Receive messages from DLQ
                    var messages = await dlqReceiver.ReceiveMessagesAsync(
                        maxMessages: Math.Min(10, maxMessages - processedCount),
                        maxWaitTime: TimeSpan.FromSeconds(2));

                    if (messages.Count == 0)
                    {
                        _logger.LogInformation("No more messages in DLQ");
                        break; // No more messages
                    }

                    _logger.LogInformation(
                        "Received {Count} messages from DLQ",
                        messages.Count);

                    foreach (var message in messages)
                    {
                        try
                        {
                            // Apply date filter
                            if (fromDate.HasValue && message.EnqueuedTime < fromDate.Value)
                            {
                                await dlqReceiver.CompleteMessageAsync(message);
                                skippedCount++;
                                _logger.LogDebug(
                                    "Skipped message (before fromDate) | MessageId: {MessageId}",
                                    message.MessageId);
                                continue;
                            }

                            if (toDate.HasValue && message.EnqueuedTime > toDate.Value)
                            {
                                await dlqReceiver.CompleteMessageAsync(message);
                                skippedCount++;
                                _logger.LogDebug(
                                    "Skipped message (after toDate) | MessageId: {MessageId}",
                                    message.MessageId);
                                continue;
                            }

                            // Apply error reason filter
                            if (!string.IsNullOrEmpty(errorReasonFilter) &&
                                !message.DeadLetterReason?.Contains(errorReasonFilter,
                                    StringComparison.OrdinalIgnoreCase) == true)
                            {
                                await dlqReceiver.CompleteMessageAsync(message);
                                skippedCount++;
                                _logger.LogDebug(
                                    "Skipped message (error reason mismatch) | MessageId: {MessageId} | Reason: {Reason}",
                                    message.MessageId,
                                    message.DeadLetterReason);
                                continue;
                            }

                            // Create new message (with fresh delivery count)
                            var newMessage = new ServiceBusMessage(message.Body)
                            {
                                ContentType = message.ContentType,
                                CorrelationId = message.CorrelationId,
                                MessageId = Guid.NewGuid().ToString(), // New ID for fresh start
                                Subject = message.Subject
                            };

                            // Copy application properties
                            foreach (var prop in message.ApplicationProperties)
                            {
                                newMessage.ApplicationProperties[prop.Key] = prop.Value;
                            }

                            // Add reprocess metadata
                            newMessage.ApplicationProperties["ReprocessedFromDLQ"] = true;
                            newMessage.ApplicationProperties["OriginalMessageId"] = message.MessageId;
                            newMessage.ApplicationProperties["ReprocessedAt"] = DateTime.UtcNow;

                            // Send to main queue
                            await sender.SendMessageAsync(newMessage);

                            // Remove from DLQ
                            await dlqReceiver.CompleteMessageAsync(message);

                            processedCount++;

                            _logger.LogInformation(
                                "Reprocessed message | OriginalId: {OriginalId} | NewId: {NewId} | DeadLetterReason: {Reason}",
                                message.MessageId,
                                newMessage.MessageId,
                                message.DeadLetterReason);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Failed to reprocess message | MessageId: {MessageId}",
                                message.MessageId);

                            // Leave in DLQ for manual investigation
                            await dlqReceiver.AbandonMessageAsync(message);
                            failedCount++;
                        }
                    }
                }

                _logger.LogInformation(
                    "[Hangfire] DLQ reprocessing complete | Processed: {Processed} | Skipped: {Skipped} | Failed: {Failed}",
                    processedCount,
                    skippedCount,
                    failedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] DLQ reprocessing failed");
                throw;
            }
        }
    }
}