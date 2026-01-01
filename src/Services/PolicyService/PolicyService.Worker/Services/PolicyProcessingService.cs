using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;
using PolicyService.Domain.Messages;
using PolicyService.Infrastructure.ExternalServices;
using PolicyService.Infrastructure.Persistence;
using Polly.CircuitBreaker;
using System.Text.Json;

namespace PolicyService.Worker.Services
{
    public class PolicyProcessingService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FileGenerationService _fileGenerationService;
        private readonly IEndBookingSystemClient _endBookingClient;
        private readonly ILogger<PolicyProcessingService> _logger;
        private readonly IConfiguration _configuration;

        public PolicyProcessingService(
            IServiceProvider serviceProvider,
            FileGenerationService fileGenerationService,
            IEndBookingSystemClient endBookingClient,
            ILogger<PolicyProcessingService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _fileGenerationService = fileGenerationService;
            _endBookingClient = endBookingClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task ProcessPolicyAsync(
            ProcessMessageEventArgs args,
            CancellationToken cancellationToken)
        {
            var messageBody = args.Message.Body.ToString();
            var correlationId = args.Message.CorrelationId ?? Guid.NewGuid().ToString();

            _logger.LogInformation(
                "Received message | MessageId: {MessageId} | CorrelationId: {CorrelationId}",
                args.Message.MessageId,
                correlationId);

            try
            {
                // Deserialize message
                var command = JsonSerializer.Deserialize<ProcessPolicyCommand>(messageBody);

                if (command == null)
                {
                    _logger.LogError("Failed to deserialize message");
                    await args.DeadLetterMessageAsync(args.Message, "Invalid message format");
                    return;
                }

                _logger.LogInformation(
                    "Processing policy {PolicyNumber} | PolicyId: {PolicyId}",
                    command.PolicyNumber,
                    command.PolicyId);

                // STEP 1: Generate .txt file
                var filePath = await _fileGenerationService.GeneratePipeDelimitedFileAsync(
                    command,
                    correlationId);

                _logger.LogInformation("File generated: {FilePath}", filePath);

                // STEP 2: Simulate end booking system processing
                bool isSuccess = false; // ✨ Declare variable outside try block
                string? externalPolicyId = null;

                try
                {
                    var bookingRequest = new BookingRequest
                    {
                        PolicyId = command.PolicyId,
                        PolicyNumber = command.PolicyNumber,
                        CustomerName = command.CustomerName,
                        Premium = command.Premium
                    };

                    _logger.LogInformation("Calling End Booking System...");

                    var bookingResponse = await _endBookingClient.SubmitPolicyAsync(bookingRequest);

                    isSuccess = bookingResponse.Success; // ✨ Assign to outer variable
                    externalPolicyId = bookingResponse.ExternalPolicyId;

                    _logger.LogInformation(
                        "End Booking System responded | Status: {Status}",
                        bookingResponse.Status);
                }
                catch (BrokenCircuitException ex)
                {
                    _logger.LogError("CIRCUIT BREAKER IS OPEN | End Booking System is down");
                    throw; // Will go to dead letter queue
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "End Booking System call failed");
                    throw; // Will retry via Service Bus
                }

                // STEP 3: Update database
                await UpdatePolicyStatusAsync(
                    command.PolicyId,
                    isSuccess,
                    filePath,
                    cancellationToken);

                // STEP 4: Complete the message
                await args.CompleteMessageAsync(args.Message, cancellationToken);

                _logger.LogInformation(
                    "Successfully processed policy {PolicyNumber} | Status: {Status}",
                    command.PolicyNumber,
                    isSuccess ? "Approved" : "Rejected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message | MessageId: {MessageId}",
                    args.Message.MessageId);

                // Check delivery count
                var deliveryCount = args.Message.DeliveryCount;
                if (deliveryCount >= 3)
                {
                    _logger.LogError(
                        "Moving to dead letter queue after {Count} attempts",
                        deliveryCount);

                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "Processing failed after multiple retries",
                        ex.Message);
                }
                else
                {
                    _logger.LogWarning(
                        "Abandoning message for retry | Attempt: {Count}/3",
                        deliveryCount);

                    await args.AbandonMessageAsync(args.Message);
                }
            }
        }

        private async Task UpdatePolicyStatusAsync(
            Guid policyId,
            bool isSuccess,
            string filePath,
            CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PolicyDbContext>();

            var policy = await dbContext.Policies.FindAsync(
                new object[] { policyId },
                cancellationToken);

            if (policy == null)
            {
                _logger.LogError("Policy {PolicyId} not found in database", policyId);
                throw new InvalidOperationException($"Policy {policyId} not found");
            }

            // Update status
            var newStatus = isSuccess ? PolicyStatus.Approved : PolicyStatus.Rejected;
            policy.SetStatus(newStatus);

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated policy {PolicyId} | Status: {Status}",
                policyId,
                policy.Status);
        }

        public Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception,
                "Service Bus error | Source: {Source}",
                args.ErrorSource);

            return Task.CompletedTask;
        }
    }
}