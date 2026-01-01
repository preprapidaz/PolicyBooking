using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyService.Domain.Interfaces
{
    // <summary>
    /// Email service contract
    /// Follows SRP: Only responsible for email operations
    /// Follows ISP: Clients only depend on methods they use
    /// </summary>
    public interface IEmailService
    {
        Task SendPolicyConfirmationAsync(string toEmail, string policyNumber);
        Task SendPolicyApprovalAsync(string toEmail, string policyNumber);
        Task SendPolicyRejectionAsync(string toEmail, string policyNumber);
    }
}
