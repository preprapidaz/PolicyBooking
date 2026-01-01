using PolicyService.Domain.Entities;

namespace PolicyService.Domain.Interfaces
{
    /// <summary>
    /// Repository abstraction - Defined in DOMAIN, implemented in INFRASTRUCTURE
    /// This is DIP in action!
    /// Domain defines WHAT it needs, Infrastructure provides HOW
    /// </summary>
    public interface IPolicyRepository
    {
        // Queries
        Task<Policy?> GetByIdAsync(Guid id);
        Task<Policy?> GetByPolicyNumberAsync(string policyNumber);
        Task<IEnumerable<Policy>> GetAllAsync();
        Task<IEnumerable<Policy>> GetPendingPoliciesAsync();
        Task<IEnumerable<Policy>> GetByCustomerEmailAsync(string email);

        // Commands
        Task<Policy> AddAsync(Policy policy);
        Task UpdateAsync(Policy policy);
        Task DeleteAsync(Guid id);

        // Utilities
        Task<bool> ExistsAsync(Guid id);
        Task<bool> PolicyNumberExistsAsync(string policyNumber);
        Task<int> GetTotalCountAsync();
    }
}