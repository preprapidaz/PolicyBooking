using PolicyService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyService.Application.Commands
{
    /// <summary>
    /// Command object - represents user intent
    /// Follows SRP: Only responsible for carrying data
    /// Part of CQRS pattern
    /// </summary>
    public class CreatePolicyCommand
    {
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public int CustomerAge { get; set; }
        public PolicyType PolicyType { get; set; }
        public decimal BasePremium { get; set; }
    }
}
