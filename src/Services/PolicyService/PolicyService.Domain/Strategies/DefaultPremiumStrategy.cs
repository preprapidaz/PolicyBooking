using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyService.Domain.Strategies
{
    /// <summary>
    /// Default premium calculation strategy
    /// Used when no specific strategy is registered for a policy type
    /// </summary>
    public class DefaultPremiumStrategy : IPremiumCalculationStrategy
    {
        public PolicyType ApplicablePolicyType => PolicyType.Health; // Not used for default

        public decimal Calculate(decimal basePremium, int customerAge, PolicyType policyType)
        {
            // Simple age-based calculation
            decimal premium = basePremium;

            if (customerAge > 60)
                premium *= 1.5m;
            else if (customerAge > 40)
                premium *= 1.2m;

            return Math.Round(premium, 2);
        }
    }
}
