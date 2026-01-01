using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace PolicyService.Infrastructure.ExternalServices
{
    /// <summary>
    /// Client for calling external End Booking System
    /// This demonstrates resilience patterns with Polly
    /// </summary>
    public class EndBookingSystemClient : IEndBookingSystemClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EndBookingSystemClient> _logger;

        public EndBookingSystemClient(
            HttpClient httpClient,
            ILogger<EndBookingSystemClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<BookingResponse> SubmitPolicyAsync(BookingRequest request)
        {
            _logger.LogInformation(
                "Calling End Booking System | PolicyId: {PolicyId}",
                request.PolicyId);

            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // This will use Polly policies configured in DI
                var response = await _httpClient.PostAsync("/posts", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<BookingResponse>(responseJson);

                    _logger.LogInformation(
                        "End Booking System response | Status: {Status}",
                        result?.Status);

                    return result ?? new BookingResponse { Success = false };
                }
                else
                {
                    _logger.LogWarning(
                        "End Booking System returned {StatusCode}",
                        response.StatusCode);

                    return new BookingResponse
                    {
                        Success = false,
                        Status = "Failed",
                        RejectionReason = $"HTTP {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "End Booking System call failed | PolicyId: {PolicyId}",
                    request.PolicyId);

                throw; // Let Polly handle retries
            }
        }
    }
}