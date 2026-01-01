namespace PolicyService.Domain.Messages
{
    /// <summary>
    /// Event published when a new policy is created
    /// This is a "fact" - something that happened
    /// </summary>
    public class PolicyCreatedEvent
    {
        public Guid PolicyId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int CustomerAge { get; set; }
        public string PolicyType { get; set; } = string.Empty;
        public decimal Premium { get; set; }
        public DateTime CreatedAt { get; set; }

        // Metadata
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime EventTimestamp { get; set; }
    }
}