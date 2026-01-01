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
    /// Factory to get the appropriate premium calculation strategy
    /// Follows OCP: New strategies can be registered without modifying this class
    /// Follows Factory Pattern
    /// </summary>
    public class PremiumCalculationStrategyFactory
    {
        private readonly Dictionary<PolicyType, IPremiumCalculationStrategy> _strategies;
        private readonly IPremiumCalculationStrategy _defaultStrategy;

        public PremiumCalculationStrategyFactory(IEnumerable<IPremiumCalculationStrategy> strategies)
        {
            _strategies = strategies
                .Where(s => s.ApplicablePolicyType != default)
                .ToDictionary(s => s.ApplicablePolicyType);

            _defaultStrategy = new DefaultPremiumStrategy();
        }

        /// <summary>
        /// Gets the appropriate strategy for the given policy type
        /// </summary>
        public IPremiumCalculationStrategy GetStrategy(PolicyType policyType)
        {
            if (_strategies.TryGetValue(policyType, out var strategy))
            {
                return strategy;
            }

            // Return default strategy if no specific one found
            return _defaultStrategy;
        }

        /// <summary>
        /// Registers a new strategy at runtime (Advanced: Hot-swappable strategies)
        /// </summary>
        public void RegisterStrategy(IPremiumCalculationStrategy strategy)
        {
            _strategies[strategy.ApplicablePolicyType] = strategy;
        }
    }
}
