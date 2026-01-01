using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;
using PolicyService.Domain.Strategies;
using PolicyService.Infrastructure.Persistence;
using PolicyService.Infrastructure.Persistence.Repositories;
using Xunit;

namespace PolicyService.Tests.Integration;

public class PolicyRepositoryTests : IDisposable
{
    private readonly PolicyDbContext _context;
    private readonly PolicyRepository _repository;
    private readonly IPremiumCalculationStrategy _healthStrategy;

    public PolicyRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PolicyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PolicyDbContext(options);

        var mockLogger = new Mock<ILogger<PolicyRepository>>();
        _repository = new PolicyRepository(_context, mockLogger.Object);

        _healthStrategy = new HealthPremiumStrategy(); //Strategy for creating policies
    }

    [Fact]
    public async Task AddAsync_ValidPolicy_SavesToDatabase()
    {
        // Arrange - Use Policy.Create factory method
        var policy = Policy.Create(
            "John Doe",
            "john@example.com",
            30,
            PolicyType.Health,
            1000m,
            _healthStrategy);

        // Act
        await _repository.AddAsync(policy);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Policies.FindAsync(policy.Id);
        saved.Should().NotBeNull();
        saved!.CustomerName.Should().Be("John Doe");
        saved.Premium.Should().Be(1000m); // Age 30, no surcharge
    }

    [Fact]
    public async Task GetByIdAsync_PolicyExists_ReturnsPolicy()
    {
        // Arrange
        var policy = await CreateAndSavePolicy("Jane Doe", 25);

        // Act
        var result = await _repository.GetByIdAsync(policy.Id);

        // Assert
        result.Should().NotBeNull();
        result!.CustomerName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task GetByIdAsync_PolicyDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPolicyNumberAsync_PolicyExists_ReturnsPolicy()
    {
        // Arrange
        var policy = await CreateAndSavePolicy("Bob Smith", 40);

        // Act
        var result = await _repository.GetByPolicyNumberAsync(policy.PolicyNumber);

        // Assert
        result.Should().NotBeNull();
        result!.PolicyNumber.Should().Be(policy.PolicyNumber);
    }

    [Fact]
    public async Task GetAllAsync_MultiplePolicies_ReturnsAll()
    {
        // Arrange
        await CreateAndSavePolicy("Alice", 30);
        await CreateAndSavePolicy("Bob", 40);
        await CreateAndSavePolicy("Charlie", 50);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.CustomerName == "Alice");
        result.Should().Contain(p => p.CustomerName == "Bob");
        result.Should().Contain(p => p.CustomerName == "Charlie");
    }

    [Fact]
    public async Task ExistsAsync_PolicyExists_ReturnsTrue()
    {
        // Arrange
        var policy = await CreateAndSavePolicy("John", 30);

        // Act
        var exists = await _repository.ExistsAsync(policy.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_PolicyDoesNotExist_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ModifiesPolicy()
    {
        // Arrange
        var policy = await CreateAndSavePolicy("John", 30);

        // Act
        policy.SetStatus(PolicyStatus.Approved);
        _context.Policies.Update(policy);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Policies.FindAsync(policy.Id);
        updated!.Status.Should().Be(PolicyStatus.Approved);
        updated.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_MultiplePolicies_AllSaved()
    {
        // Arrange
        await CreateAndSavePolicy("Policy1", 30);
        await CreateAndSavePolicy("Policy2", 40);

        // Act
        var all = await _repository.GetAllAsync();

        // Assert
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByPolicyNumberAsync_CaseInsensitive_FindsPolicy()
    {
        // Arrange
        var policy = await CreateAndSavePolicy("John", 30);
        var lowerCasePolicyNumber = policy.PolicyNumber.ToLower();

        // Act
        var result = await _repository.GetByPolicyNumberAsync(lowerCasePolicyNumber);

        // Assert
        result.Should().NotBeNull();
    }

    // Helper method
    private async Task<Policy> CreateAndSavePolicy(string name, int age)
    {
        var policy = Policy.Create(
            name,
            $"{name.ToLower().Replace(" ", "")}@example.com",
            age,
            PolicyType.Health,
            1000m,
            _healthStrategy);

        _context.Policies.Add(policy);
        await _context.SaveChangesAsync();
        return policy;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}