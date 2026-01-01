namespace PolicyService.Domain.Messages
{
    /// <summary>
    /// Event published when policy processing completes
    /// </summary>
    public class PolicyProcessedEvent
    {
        public Guid PolicyId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Approved, Rejected
        public string? RejectionReason { get; set; }
        public DateTime ProcessedAt { get; set; }

        // End booking system details
        public string? ExternalPolicyId { get; set; }
        public string? ExternalPolicyNumber { get; set; }

        // Metadata
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime EventTimestamp { get; set; }
    }
}