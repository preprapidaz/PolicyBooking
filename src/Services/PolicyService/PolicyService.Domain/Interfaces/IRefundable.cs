namespace PolicyService.Domain.Interfaces
{
    /// <summary>
    /// Interface for policies that support refunds
    /// ISP: Small, focused interface - only refund-related operations
    /// </summary>
    public interface IRefundable
    {
        decimal CalculateRefund();
        bool IsRefundable { get; }
        decimal RefundPercentage { get; }
    }
}