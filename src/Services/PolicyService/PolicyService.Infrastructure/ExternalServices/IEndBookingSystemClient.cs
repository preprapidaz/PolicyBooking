namespace PolicyService.Infrastructure.ExternalServices
{
    public interface IEndBookingSystemClient
    {
        Task<BookingResponse> SubmitPolicyAsync(BookingRequest request);
    }

    public class BookingRequest
    {
        public Guid PolicyId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public decimal Premium { get; set; }
    }

    public class BookingResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ExternalPolicyId { get; set; }
        public string? ExternalPolicyNumber { get; set; }
        public string? RejectionReason { get; set; }
    }
}