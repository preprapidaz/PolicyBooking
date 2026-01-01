using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;

namespace PolicyService.Domain.Entities
{
    /// <summary>
    /// Base Policy entity - NO assumptions about refundability or cancellation
    /// Follows LSP: Base class doesn't force behavior that subclasses can't fulfill
    /// </summary>
    public class Policy
    {
        public Guid Id { get; private set; }
        public string PolicyNumber { get; private set; }
        public string CustomerName { get; private set; }
        public string CustomerEmail { get; private set; }
        public int CustomerAge { get; private set; }
        public PolicyType PolicyType { get; private set; }
        public decimal Premium { get; private set; }
        public PolicyStatus Status { get; protected set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ProcessedAt { get; private set; }

        protected Policy() { } // Protected constructor for inheritance

        public static Policy Create(
            string customerName,
            string customerEmail,
            int customerAge,
            PolicyType policyType,
            decimal basePremium,
            IPremiumCalculationStrategy premiumStrategy)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(customerName))
                throw new ArgumentException("Customer name is required", nameof(customerName));

            if (string.IsNullOrWhiteSpace(customerEmail))
                throw new ArgumentException("Customer email is required", nameof(customerEmail));

            if (customerAge < 18)
                throw new ArgumentException("Customer must be 18 or older", nameof(customerAge));

            if (basePremium <= 0)
                throw new ArgumentException("Base premium must be greater than zero", nameof(basePremium));

            var calculatedPremium = premiumStrategy.Calculate(basePremium, customerAge, policyType);

            var policy = new Policy
            {
                Id = Guid.NewGuid(),
                PolicyNumber = GeneratePolicyNumber(),
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                CustomerAge = customerAge,
                PolicyType = policyType,
                Premium = calculatedPremium,
                Status = PolicyStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            return policy;
        }

        private static string GeneratePolicyNumber()
        {
            return $"POL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }

        public void Approve()
        {
            if (Status != PolicyStatus.Pending)
                throw new InvalidOperationException("Only pending policies can be approved");

            Status = PolicyStatus.Approved;
            ProcessedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            if (Status != PolicyStatus.Pending)
                throw new InvalidOperationException("Only pending policies can be rejected");

            Status = PolicyStatus.Rejected;
            ProcessedAt = DateTime.UtcNow;
        }

        public void SetStatus(PolicyStatus status)
        {
            Status = status;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}