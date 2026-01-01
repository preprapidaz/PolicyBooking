using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyService.Domain.Interfaces
{
    /// <summary>
    /// Interface for policies that can be cancelled
    /// Follows LSP: Clear contract for cancellation behavior
    /// </summary>
    public interface ICancellablePolicy
    {
        /// <summary>
        /// Cancels the policy
        /// </summary>
        /// <param name="reason">Reason for cancellation</param>
        void Cancel(string reason);

        /// <summary>
        /// Indicates if the policy can currently be cancelled
        /// </summary>
        bool CanBeCancelled { get; }

        /// <summary>
        /// Date when the policy was cancelled (null if not cancelled)
        /// </summary>
        DateTime? CancelledAt { get; }

        /// <summary>
        /// Reason for cancellation
        /// </summary>
        string CancellationReason { get; }
    }
}
