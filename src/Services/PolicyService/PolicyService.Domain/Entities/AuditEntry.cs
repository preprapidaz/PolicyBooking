namespace PolicyService.Domain.Entities
{
    /// <summary>
    /// Audit trail entry for tracking entity changes
    /// Used by IAuditable interface
    /// </summary>
    public class AuditEntry
    {
        public Guid Id { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string PerformedBy { get; set; }
    }
}