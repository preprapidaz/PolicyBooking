namespace PolicyService.Domain.Interfaces
{
    /// <summary>
    /// Unit of Work pattern - Manages database transactions
    /// Abstraction for transaction management
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IPolicyRepository Policies { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}