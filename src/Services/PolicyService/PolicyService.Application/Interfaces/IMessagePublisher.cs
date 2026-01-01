namespace PolicyService.Domain.Interfaces
{
    /// <summary>
    /// Abstraction for publishing messages to message broker
    /// Follows Dependency Inversion Principle
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// Send message to a queue
        /// </summary>
        Task SendMessageAsync<T>(string queueName, T message, string? correlationId = null);

        /// <summary>
        /// Publish message to a topic (pub/sub)
        /// </summary>
        Task PublishEventAsync<T>(string topicName, T message, string? correlationId = null);

        /// <summary>
        /// Send batch of messages to a queue
        /// </summary>
        Task SendBatchAsync<T>(string queueName, IEnumerable<T> messages);

        /// <summary>
        /// Schedule message for future delivery
        /// </summary>
        Task ScheduleMessageAsync<T>(string queueName, T message, DateTimeOffset scheduledTime);
    }
}