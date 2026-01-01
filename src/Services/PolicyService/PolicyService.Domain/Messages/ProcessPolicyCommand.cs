namespace PolicyService.Domain.Messages
{
    /// <summary>
    /// Command to process a policy (send to end booking system)
    /// This is an "instruction" - do something
    /// </summary>
    public class ProcessPolicyCommand
    {
        public Guid PolicyId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int CustomerAge { get; set; }
        public string PolicyType { get; set; } = string.Empty;
        public decimal Premium { get; set; }

        // Metadata
        public string CorrelationId { get; set; } = string.Empty;
        public int RetryCount { get; set; }
    }
}