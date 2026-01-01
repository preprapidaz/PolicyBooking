namespace PolicyService.Domain.Interfaces
{
    /// <summary>
    /// Interface for policies that can be cancelled
    /// ISP: Separate from refund logic - focused responsibility
    /// </summary>
    public interface ICancellable
    {
        Task Cancel(string reason);
        bool CanBeCancelled { get; }
        DateTime? CancelledAt { get; }
        string CancellationReason { get; }
    }
}