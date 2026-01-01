using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PolicyService.Application.Commands;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;
using PolicyService.Domain.Strategies;
using Xunit;

namespace PolicyService.Tests.Application;

public class CreatePolicyCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsResponse()
    {
        // Arrange
        var mockRepo = new Mock<IPolicyRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockPublisher = new Mock<IMessagePublisher>();
        var mockLogger = new Mock<ILogger<CreatePolicyCommandHandler>>();

        var strategies = new List<IPremiumCalculationStrategy>
        {
            new HealthPremiumStrategy(),
            new LifePremiumStrategy()
        };
        var factory = new PremiumCalculationStrategyFactory(strategies);

        var handler = new CreatePolicyCommandHandler(
            mockRepo.Object,
            mockUnitOfWork.Object,
            factory,
            mockPublisher.Object,
            mockLogger.Object);

        var command = new CreatePolicyCommand
        {
            CustomerName = "John Doe",
            CustomerEmail = "john@test.com",
            CustomerAge = 30,
            PolicyType = PolicyType.Health,
            BasePremium = 1000m
        };

        mockRepo.Setup(r => r.AddAsync(It.IsAny<Policy>()))
            .ReturnsAsync((Policy p) => p);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Premium.Should().Be(1000m);
    }
}