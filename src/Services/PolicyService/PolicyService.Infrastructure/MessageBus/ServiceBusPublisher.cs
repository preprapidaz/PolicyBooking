using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PolicyService.Domain.Interfaces;
using System.Text.Json;

namespace PolicyService.Infrastructure.Messaging
{
    /// <summary>
    /// Azure Service Bus implementation of message publisher
    /// </summary>
    public class ServiceBusPublisher : IMessagePublisher, IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusPublisher> _logger;
        private readonly Dictionary<string, ServiceBusSender> _senders;
        private readonly object _lock = new();

        public ServiceBusPublisher(
            IConfiguration configuration,
            ILogger<ServiceBusPublisher> logger)
        {
            var connectionString = configuration["AzureServiceBusConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Service Bus connection string not configured");
            }

            _client = new ServiceBusClient(connectionString);
            _logger = logger;
            _senders = new Dictionary<string, ServiceBusSender>();

            _logger.LogInformation("Service Bus client initialized");
        }

        /// <summary>
        /// Send message to queue
        /// </summary>
        public async Task SendMessageAsync<T>(
            string queueName,
            T message,
            string? correlationId = null)
        {
            try
            {
                var sender = GetOrCreateSender(queueName);

                var messageBody = JsonSerializer.Serialize(message);
                var serviceBusMessage = new ServiceBusMessage(messageBody)
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
                };

                await sender.SendMessageAsync(serviceBusMessage);

                _logger.LogInformation(
                    "Message sent to queue: {Queue} | MessageId: {MessageId} | CorrelationId: {CorrelationId}",
                    queueName,
                    serviceBusMessage.MessageId,
                    serviceBusMessage.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send message to queue: {Queue}",
                    queueName);
                throw;
            }
        }

        /// <summary>
        /// Publish event to topic (pub/sub)
        /// </summary>
        public async Task PublishEventAsync<T>(
            string topicName,
            T message,
            string? correlationId = null)
        {
            try
            {
                var sender = GetOrCreateSender(topicName);

                var messageBody = JsonSerializer.Serialize(message);
                var serviceBusMessage = new ServiceBusMessage(messageBody)
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
                    Subject = typeof(T).Name, // Event type
                };

                await sender.SendMessageAsync(serviceBusMessage);

                _logger.LogInformation(
                    "Event published to topic: {Topic} | EventType: {EventType} | MessageId: {MessageId}",
                    topicName,
                    typeof(T).Name,
                    serviceBusMessage.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish event to topic: {Topic}",
                    topicName);
                throw;
            }
        }

        /// <summary>
        /// Send batch of messages
        /// </summary>
        public async Task SendBatchAsync<T>(string queueName, IEnumerable<T> messages)
        {
            try
            {
                var sender = GetOrCreateSender(queueName);

                using var messageBatch = await sender.CreateMessageBatchAsync();

                int count = 0;
                foreach (var message in messages)
                {
                    var messageBody = JsonSerializer.Serialize(message);
                    var serviceBusMessage = new ServiceBusMessage(messageBody)
                    {
                        ContentType = "application/json",
                        MessageId = Guid.NewGuid().ToString(),
                    };

                    if (!messageBatch.TryAddMessage(serviceBusMessage))
                    {
                        throw new InvalidOperationException(
                            $"Message {count} is too large for the batch");
                    }

                    count++;
                }

                await sender.SendMessagesAsync(messageBatch);

                _logger.LogInformation(
                    "Batch sent to queue: {Queue} | Count: {Count}",
                    queueName,
                    count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send batch to queue: {Queue}",
                    queueName);
                throw;
            }
        }

        /// <summary>
        /// Schedule message for future delivery
        /// </summary>
        public async Task ScheduleMessageAsync<T>(
            string queueName,
            T message,
            DateTimeOffset scheduledTime)
        {
            try
            {
                var sender = GetOrCreateSender(queueName);

                var messageBody = JsonSerializer.Serialize(message);
                var serviceBusMessage = new ServiceBusMessage(messageBody)
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                };

                await sender.ScheduleMessageAsync(serviceBusMessage, scheduledTime);

                _logger.LogInformation(
                    "Message scheduled for {Time} in queue: {Queue}",
                    scheduledTime,
                    queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to schedule message in queue: {Queue}",
                    queueName);
                throw;
            }
        }

        /// <summary>
        /// Get or create sender for queue/topic
        /// Thread-safe singleton pattern
        /// </summary>
        private ServiceBusSender GetOrCreateSender(string queueOrTopicName)
        {
            if (_senders.TryGetValue(queueOrTopicName, out var existingSender))
            {
                return existingSender;
            }

            lock (_lock)
            {
                // Double-check locking pattern
                if (_senders.TryGetValue(queueOrTopicName, out existingSender))
                {
                    return existingSender;
                }

                var newSender = _client.CreateSender(queueOrTopicName);
                _senders[queueOrTopicName] = newSender;

                _logger.LogInformation(
                    "Created sender for: {QueueOrTopic}",
                    queueOrTopicName);

                return newSender;
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            foreach (var sender in _senders.Values)
            {
                await sender.DisposeAsync();
            }

            await _client.DisposeAsync();

            _logger.LogInformation("Service Bus client disposed");
        }
    }
}