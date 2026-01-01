using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;

namespace PolicyService.Domain.Entities
{
    /// <summary>
    /// Term Life Policy - Minimal interface implementation
    /// ISP COMPLIANT: Only implements what it needs!
    /// No IRefundable (not refundable)
    /// No IClaimable (claims handled differently)
    /// No IRenewable (fixed term)
    /// </summary>
    public class TermLifePolicy : Policy,
        ICancellable,     // ✅ Can be cancelled
        IAuditable        // ✅ Needs audit trail
                          // Notice: NO IRefundable, IClaimable, or IRenewable!
    {
        public int TermYears { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public string CancellationReason { get; private set; }
        public DateTime LastModifiedAt { get; private set; }
        public string LastModifiedBy { get; private set; }

        // ICancellable implementation
        public bool CanBeCancelled => Status == PolicyStatus.Approved && CancelledAt == null;

        public async Task Cancel(string reason)
        {
            if (!CanBeCancelled)
                throw new InvalidOperationException("Cannot cancel this policy");

            CancelledAt = DateTime.UtcNow;
            CancellationReason = reason;
            Status = PolicyStatus.Cancelled;

            await LogAuditEntry("CANCELLED", $"Reason: {reason}");
        }

        // IAuditable implementation
        private List<AuditEntry> _auditTrail = new();

        public async Task LogAuditEntry(string action, string details)
        {
            var entry = new AuditEntry
            {
                Id = Guid.NewGuid(),
                EntityId = Id,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow,
                PerformedBy = "System"
            };

            _auditTrail.Add(entry);
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = "System";

            await Task.CompletedTask;
        }

        public async Task<IEnumerable<AuditEntry>> GetAuditHistory()
        {
            return await Task.FromResult(_auditTrail.AsEnumerable());
        }
    }
}