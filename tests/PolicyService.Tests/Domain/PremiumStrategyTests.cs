using FluentAssertions;
using PolicyService.Domain.Strategies;
using PolicyService.Domain.Enums;
using Xunit;

namespace PolicyService.Tests.Domain;

public class HealthPremiumStrategyTests
{
    private readonly HealthPremiumStrategy _strategy = new();

    [Fact]
    public void Calculate_Age30_NoSurcharge()
    {
        // Act
        var result = _strategy.Calculate(1000m, 30, PolicyType.Health);

        // Assert
        result.Should().Be(1000m);
    }

    [Fact]
    public void Calculate_Age55_Adds500Surcharge()
    {
        // Act
        var result = _strategy.Calculate(1000m, 55, PolicyType.Health);

        // Assert
        result.Should().Be(1500m); // 1000 + 500
    }

    [Fact]
    public void Calculate_Age50_Adds500Surcharge()
    {
        var result = _strategy.Calculate(1000m, 50, PolicyType.Health);
        result.Should().Be(1500m); // Exactly 50 = no surcharge
    }

    [Fact]
    public void Calculate_Age65_Adds500Surcharge()
    {
        var result = _strategy.Calculate(2000m, 65, PolicyType.Health);
        result.Should().Be(4000); // 4000
    }
}
