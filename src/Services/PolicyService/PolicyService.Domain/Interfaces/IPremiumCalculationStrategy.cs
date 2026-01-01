using PolicyService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyService.Domain.Interfaces
{
    /// <summary>
    /// Strategy interface for premium calculation
    /// Follows OCP: New strategies can be added without modifying existing code
    /// Follows ISP: Single focused interface
    /// </summary>
    public interface IPremiumCalculationStrategy
    {
        // <summary>
        /// Calculates premium based on policy details
        /// </summary>
        decimal Calculate(decimal basePremium, int customerAge, PolicyType policyType);

        /// <summary>
        /// Indicates which policy type this strategy handles
        /// </summary>
        PolicyType ApplicablePolicyType { get; }
    }
}
