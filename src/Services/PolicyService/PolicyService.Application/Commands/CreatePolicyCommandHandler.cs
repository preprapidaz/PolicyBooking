using Microsoft.Extensions.Logging;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;
using PolicyService.Domain.Messages;
using PolicyService.Domain.Strategies;

namespace PolicyService.Application.Commands
{
    /// <summary>
    /// Command Handler - Depends ONLY on abstractions (interfaces)
    /// DIP COMPLIANT: No knowledge of SQL, EF Core, or any infrastructure
    /// </summary>
    public class CreatePolicyCommandHandler
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PremiumCalculationStrategyFactory _strategyFactory;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<CreatePolicyCommandHandler> _logger;

        // Constructor injection - all dependencies are ABSTRACTIONS
        public CreatePolicyCommandHandler(
            IPolicyRepository policyRepository,
            IUnitOfWork unitOfWork,
            PremiumCalculationStrategyFactory strategyFactory,
            IMessagePublisher messagePublisher,
            ILogger<CreatePolicyCommandHandler> logger)
        {
            _policyRepository = policyRepository;
            _unitOfWork = unitOfWork;
            _strategyFactory = strategyFactory;
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        public async Task<CreatePolicyResponse> HandleAsync(CreatePolicyCommand command)
        {
            _logger.LogInformation("Creating policy for {CustomerName}", command.CustomerName);

            try
            {
                // 1. Get appropriate premium calculation strategy
                var strategy = _strategyFactory.GetStrategy(command.PolicyType);

                // 2. Create domain entity (pure business logic)
                var policy = Policy.Create(
                    command.CustomerName,
                    command.CustomerEmail,
                    command.CustomerAge,
                    command.PolicyType,
                    command.BasePremium,
                    strategy
                );

                // 3. Save using repository abstraction
                await _policyRepository.AddAsync(policy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Policy created successfully: {PolicyNumber}", policy.PolicyNumber);

                //NEW: Publish to Service Bus
                var correlationId = Guid.NewGuid().ToString();

                try
                {
                    // 1. Send command to processing queue
                    var processingCommand = new ProcessPolicyCommand
                    {
                        PolicyId = policy.Id,
                        PolicyNumber = policy.PolicyNumber,
                        CustomerName = policy.CustomerName,
                        CustomerEmail = policy.CustomerEmail,
                        CustomerAge = policy.CustomerAge,
                        PolicyType = policy.PolicyType.ToString(),
                        Premium = policy.Premium,
                        CorrelationId = correlationId,
                        RetryCount = 0
                    };

                    await _messagePublisher.SendMessageAsync(
                        "policy-processing-queue",
                        processingCommand,
                        correlationId);

                    // 2. Publish event to topic (for other services)
                    var policyCreatedEvent = new PolicyCreatedEvent
                    {
                        PolicyId = policy.Id,
                        PolicyNumber = policy.PolicyNumber,
                        CustomerName = policy.CustomerName,
                        CustomerEmail = policy.CustomerEmail,
                        CustomerAge = policy.CustomerAge,
                        PolicyType = policy.PolicyType.ToString(),
                        Premium = policy.Premium,
                        CreatedAt = policy.CreatedAt,
                        CorrelationId = correlationId,
                        EventTimestamp = DateTime.UtcNow
                    };

                    await _messagePublisher.PublishEventAsync(
                        "policy-events",
                        policyCreatedEvent,
                        correlationId);

                    _logger.LogInformation(
                        "Messages published for policy {PolicyId} | CorrelationId: {CorrelationId}",
                        policy.Id,
                        correlationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to publish messages for policy {PolicyId}, but policy was saved",
                        policy.Id);

                    // Don't fail the request - policy is saved
                    // Messages will be published via retry mechanism or manual intervention
                }


                // 4. Return response
                return new CreatePolicyResponse
                {
                    PolicyId = policy.Id,
                    PolicyNumber = policy.PolicyNumber,
                    Status = policy.Status.ToString(),
                    Premium = policy.Premium,
                    Message = "Policy created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating policy for {CustomerName}", command.CustomerName);
                throw;
            }
        }
    }

    public class CreatePolicyResponse
    {
        public Guid PolicyId { get; set; }
        public string PolicyNumber { get; set; }
        public string Status { get; set; }
        public decimal Premium { get; set; }
        public string Message { get; set; }
    }
}