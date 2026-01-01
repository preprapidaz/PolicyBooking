using PolicyService.Domain.Interfaces;
using PolicyService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace PolicyService.Application.Services
{
    /// <summary>
    /// Service for handling policy refunds
    /// Follows LSP: Works with IRefundablePolicy interface
    /// Only processes policies that CAN be refunded
    /// </summary>
    public class RefundService
    {
        private readonly ILogger<RefundService> _logger;

        public RefundService(ILogger<RefundService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process refund for a refundable policy
        /// LSP COMPLIANT: Accepts interface, not base class
        /// </summary>
        public async Task<RefundResult> ProcessRefund(IRefundablePolicy policy)
        {
            // Check if policy is refundable
            if (!policy.IsRefundable)
            {
                _logger.LogWarning("Refund attempted on non-refundable policy");
                return new RefundResult
                {
                    Success = false,
                    Message = "This policy is not eligible for refund",
                    RefundAmount = 0m
                };
            }

            // Calculate refund
            var refundAmount = policy.CalculateRefund();

            _logger.LogInformation(
                "Processing refund of {Amount} ({Percentage}%)",
                refundAmount,
                policy.RefundPercentage * 100);

            // Process refund (integrate with payment gateway)
            // await _paymentGateway.ProcessRefund(refundAmount);

            return new RefundResult
            {
                Success = true,
                Message = $"Refund of {refundAmount:C} processed successfully",
                RefundAmount = refundAmount
            };
        }

        /// <summary>
        /// Check if a policy is refundable
        /// Works with ANY policy type
        /// </summary>
        public RefundEligibility CheckRefundEligibility(Policy policy)
        {
            // Check if policy implements IRefundablePolicy
            if (policy is IRefundablePolicy refundablePolicy)
            {
                return new RefundEligibility
                {
                    IsEligible = refundablePolicy.IsRefundable,
                    RefundAmount = refundablePolicy.CalculateRefund(),
                    RefundPercentage = refundablePolicy.RefundPercentage,
                    Message = refundablePolicy.IsRefundable
                        ? $"Eligible for {refundablePolicy.RefundPercentage:P0} refund"
                        : "Not eligible for refund"
                };
            }

            // Policy doesn't support refunds
            return new RefundEligibility
            {
                IsEligible = false,
                RefundAmount = 0m,
                RefundPercentage = 0m,
                Message = "This policy type does not support refunds"
            };
        }
    }

    public class RefundResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class RefundEligibility
    {
        public bool IsEligible { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal RefundPercentage { get; set; }
        public string Message { get; set; }
    }
}