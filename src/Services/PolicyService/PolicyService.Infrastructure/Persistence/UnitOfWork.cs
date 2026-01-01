using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PolicyService.Domain.Interfaces;
using PolicyService.Infrastructure.Persistence.Repositories;

namespace PolicyService.Infrastructure.Persistence
{
    /// <summary>
    /// Unit of Work implementation
    /// Manages transactions and coordinates multiple repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PolicyDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction? _transaction;

        public IPolicyRepository Policies { get; }

        public UnitOfWork(
            PolicyDbContext context,
            ILogger<UnitOfWork> logger,
            IPolicyRepository policyRepository)
        {
            _context = context;
            _logger = logger;
            Policies = policyRepository;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogDebug("Database transaction started");
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _transaction?.CommitAsync()!;
                _logger.LogDebug("Database transaction committed");
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                await _transaction?.RollbackAsync()!;
                _logger.LogDebug("Database transaction rolled back");
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}