using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Interfaces;

namespace PolicyService.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// SQL Server implementation of IPolicyRepository
    /// DIP: This is the LOW-LEVEL detail that implements the HIGH-LEVEL abstraction
    /// Can be swapped with MongoDbPolicyRepository, RedisPolicyRepository, etc.
    /// </summary>
    public class PolicyRepository : IPolicyRepository
    {
        private readonly PolicyDbContext _context;
        private readonly ILogger<PolicyRepository> _logger;

        public PolicyRepository(PolicyDbContext context, ILogger<PolicyRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Policy?> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("Fetching policy by ID: {PolicyId}", id);
            return await _context.Policies.FindAsync(id);
        }

        public async Task<Policy?> GetByPolicyNumberAsync(string policyNumber)
        {
            _logger.LogDebug("Fetching policy by number: {PolicyNumber}", policyNumber);
            return await _context.Policies.FirstOrDefaultAsync(p =>
            p.PolicyNumber.Equals(policyNumber, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<Policy>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all policies");
            return await _context.Policies.ToListAsync();
        }

        public async Task<IEnumerable<Policy>> GetPendingPoliciesAsync()
        {
            _logger.LogDebug("Fetching pending policies");
            return await _context.Policies
                .Where(p => p.Status == Domain.Enums.PolicyStatus.Pending)
                .ToListAsync();
        }

        public async Task<IEnumerable<Policy>> GetByCustomerEmailAsync(string email)
        {
            _logger.LogDebug("Fetching policies for customer: {Email}", email);
            return await _context.Policies
                .Where(p => p.CustomerEmail == email)
                .ToListAsync();
        }

        public async Task<Policy> AddAsync(Policy policy)
        {
            _logger.LogDebug("Adding new policy: {PolicyNumber}", policy.PolicyNumber);
            await _context.Policies.AddAsync(policy);
            return policy;
        }

        public async Task UpdateAsync(Policy policy)
        {
            _logger.LogDebug("Updating policy: {PolicyId}", policy.Id);
            _context.Policies.Update(policy);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogDebug("Deleting policy: {PolicyId}", id);
            var policy = await GetByIdAsync(id);
            if (policy != null)
            {
                _context.Policies.Remove(policy);
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Policies.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> PolicyNumberExistsAsync(string policyNumber)
        {
            return await _context.Policies
                .AnyAsync(p => p.PolicyNumber == policyNumber);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Policies.CountAsync();
        }
    }
}