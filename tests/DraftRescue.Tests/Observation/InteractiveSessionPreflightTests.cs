using DraftRescue.Platform.Windows.Testing;
using Xunit;

namespace DraftRescue.Tests.Observation;

public sealed class InteractiveSessionPreflightTests
{
    [Fact]
    public void Capture_IsContentFreeAndReadyFlagIsFailClosed()
    {
        var result = InteractiveSessionPreflightResult.Capture();

        Assert.True(result.CurrentProcessId > 0);
        Assert.True(result.CurrentSessionId >= 0);
        if (result.ForegroundProcessId == 0)
        {
            Assert.Null(result.ForegroundProcessSessionId);
        }

        if (result.ReadyForInteractiveFixture)
        {
            Assert.True(result.UserInteractive);
            Assert.True(result.ForegroundWindowPresent);
            Assert.True(result.ForegroundSameSession);
            Assert.True(result.InputDesktopAccessible);
            Assert.True(result.ForegroundFocusWindowPresent);
        }
    }
}
