using FsCheck;

namespace ExchangeRateApi.Tests;

/// <summary>
/// Base class for property-based tests with FsCheck configuration
/// </summary>
public abstract class PropertyTestBase
{
    /// <summary>
    /// Helper method to run property tests
    /// Use [Property(MaxTest = 100)] attribute on test methods for 100 iterations
    /// </summary>
    protected static void CheckProperty(Property property, string testName)
    {
        Check.Quick(property);
    }
}