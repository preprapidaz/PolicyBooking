using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;
using System.Security.Claims;

namespace PolicyService.Domain.Entities
{
    /// <summary>
    /// Health Policy - Implements multiple focused interfaces
    /// ISP COMPLIANT: Only implements what it actually supports
    /// </summary>
    public class HealthPolicy : Policy,
        IRefundable,      // ✅ Supports refunds
        ICancellable,     // ✅ Can be cancelled
        IAuditable        // ✅ Needs audit trail
    {
        // Refund-related properties
        public bool HasBeenUsed { get; private set; }

        // Cancellation properties
        public DateTime? CancelledAt { get; private set; }
        public string CancellationReason { get; private set; }

        // Audit properties
        public DateTime LastModifiedAt { get; private set; }
        public string LastModifiedBy { get; private set; }

        // IRefundable implementation
        public bool IsRefundable => Status == PolicyStatus.Approved && !HasBeenUsed;
        public decimal RefundPercentage => HasBeenUsed ? 0m : 0.8m;

        public decimal CalculateRefund()
        {
            return IsRefundable ? Premium * RefundPercentage : 0m;
        }

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
                PerformedBy = "System" // In real app, get from current user
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