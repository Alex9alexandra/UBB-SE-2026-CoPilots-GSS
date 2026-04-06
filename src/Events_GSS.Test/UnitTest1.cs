using Xunit;

namespace Boards_WP.Tests;

public class SanityCheck
{
    [Fact]
    public void System_Should_Work()
    {
        // This test always passes. 
        // If it runs in CI, we know our build pipeline is working!
        bool isSetupCorrect = true;
        Assert.True(isSetupCorrect);
    }
}