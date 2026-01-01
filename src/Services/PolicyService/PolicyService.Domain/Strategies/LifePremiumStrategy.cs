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
    /// Premium calculation strategy for Life Insurance
    /// </summary>
    public class LifePremiumStrategy : IPremiumCalculationStrategy
    {
        public PolicyType ApplicablePolicyType => PolicyType.Life;

        public decimal Calculate(decimal basePremium, int customerAge, PolicyType policyType)
        {
            if (policyType != PolicyType.Life)
                throw new InvalidOperationException("This strategy only handles Life insurance");

            decimal premium = basePremium;

            // Life insurance has different risk assessment
            if (customerAge >= 75)
            {
                premium *= 3.0m; // Extremely high risk
            }
            else if (customerAge >= 65)
            {
                premium *= 2.2m; // Very high risk
            }
            else if (customerAge >= 55)
            {
                premium *= 1.7m; // High risk
            }
            else if (customerAge >= 45)
            {
                premium *= 1.3m; // Medium risk
            }
            else if (customerAge >= 35)
            {
                premium *= 1.1m; // Low risk
            }
            // Young people (under 35) get base premium

            return Math.Round(premium, 2);
        }
    }
}
