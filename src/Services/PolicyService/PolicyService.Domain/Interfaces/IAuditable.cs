using PolicyService.Domain.Entities;

namespace PolicyService.Domain.Interfaces
{
    /// <summary>
    /// Interface for entities that need audit trails
    /// ISP: Audit-specific operations
    /// </summary>
    public interface IAuditable
    {
        Task LogAuditEntry(string action, string details);
        Task<IEnumerable<AuditEntry>> GetAuditHistory();
        DateTime LastModifiedAt { get; }
        string LastModifiedBy { get; }
    }
}