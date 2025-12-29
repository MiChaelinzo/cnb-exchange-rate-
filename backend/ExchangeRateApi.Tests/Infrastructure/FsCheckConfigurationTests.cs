using FsCheck;
using FsCheck.Xunit;

namespace ExchangeRateApi.Tests.Infrastructure;

/// <summary>
/// Tests to verify FsCheck property-based testing framework is properly configured
/// Feature: exchange-rate-display, Infrastructure: FsCheck Configuration
/// </summary>
public class FsCheckConfigurationTests : PropertyTestBase
{
    [Property(MaxTest = 100)]
    public Property BasicArithmeticProperty()
    {
        return Prop.ForAll<int, int>((a, b) => a + b == b + a);
    }

    [Property(MaxTest = 100)]
    public Property StringLengthProperty()
    {
        return Prop.ForAll<string>(s => 
            s != null ? s.Length >= 0 : true);
    }

    [Fact]
    public void FsCheckConfiguration_ShouldBeProperlyConfigured()
    {
        // Verify that FsCheck is working by running a simple property test
        var property = Prop.ForAll<int>(x => x + 0 == x);
        CheckProperty(property, "Identity property");
        
        // If we reach here, FsCheck is properly configured
        Assert.True(true);
    }
}