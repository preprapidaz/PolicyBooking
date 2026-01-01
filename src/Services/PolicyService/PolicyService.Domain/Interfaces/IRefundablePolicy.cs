using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyService.Domain.Interfaces
{
    /// <summary>
    /// Interface for policies that support refunds
    /// Follows LSP: Only policies that CAN be refunded implement this
    /// Follows ISP: Separate interface for refund capability
    /// </summary>
    public interface IRefundablePolicy
    {
        /// <summary>
        /// Calculates the refund amount for this policy
        /// </summary>
        /// <returns>Refund amount. Never returns null or throws exception.</returns>
        decimal CalculateRefund();

        /// <summary>
        /// Indicates if the policy is currently refundable
        /// </summary>
        bool IsRefundable { get; }

        /// <summary>
        /// Gets the refund percentage (0.0 to 1.0)
        /// </summary>
        decimal RefundPercentage { get; }
    }
}
