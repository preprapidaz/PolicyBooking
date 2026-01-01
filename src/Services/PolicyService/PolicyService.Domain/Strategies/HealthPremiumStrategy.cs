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
    /// Premium calculation strategy for Health Insurance
    /// Follows OCP: This is a new strategy, we didn't modify existing code!
    /// Follows SRP: Only responsible for health insurance premium logic
    /// </summary>
    public class HealthPremiumStrategy : IPremiumCalculationStrategy
    {
        public PolicyType ApplicablePolicyType => PolicyType.Health;

        public decimal Calculate(decimal basePremium, int customerAge, PolicyType policyType)
        {
            if (policyType != PolicyType.Health)
                throw new InvalidOperationException("This strategy only handles Health insurance");

            decimal premium = basePremium;

            // Health-specific age risk factors
            if (customerAge >= 70)
            {
                premium *= 2.5m; // Very high risk
            }
            else if (customerAge >= 60)
            {
                premium *= 2.0m; // High risk
            }
            else if (customerAge >= 50)
            {
                premium *= 1.5m; // Medium risk
            }
            else if (customerAge >= 40)
            {
                premium *= 1.2m; // Low risk
            }

            // Additional health-specific factors could be added here
            // e.g., pre-existing conditions, coverage amount, etc.

            return Math.Round(premium, 2);
        }
    }
}
