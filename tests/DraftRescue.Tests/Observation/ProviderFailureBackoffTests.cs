using System.Diagnostics;
using DraftRescue.Platform.Windows.Reliability;
using Xunit;

namespace DraftRescue.Tests.Observation;

public sealed class ProviderFailureBackoffTests
{
    [Fact]
    public void FailureStreak_GrowsToBoundAndSuccessResets()
    {
        long timestamp = 0;
        var backoff = new ProviderFailureBackoff(() => timestamp);

        Assert.True(backoff.TryAcquire());
        Assert.Equal(250, backoff.RecordFailure());
        Assert.False(backoff.TryAcquire());
        Assert.Equal(1, backoff.SuppressedCount);

        timestamp += (long)(0.25d * Stopwatch.Frequency);
        Assert.True(backoff.TryAcquire());
        Assert.Equal(500, backoff.RecordFailure());
        Assert.Equal(1000, backoff.RecordFailure());
        Assert.Equal(2000, backoff.RecordFailure());
        Assert.Equal(4000, backoff.RecordFailure());
        Assert.Equal(5000, backoff.RecordFailure());
        Assert.Equal(5000, backoff.RecordFailure());

        backoff.RecordSuccess();
        Assert.Equal(0, backoff.RemainingMilliseconds);
        Assert.True(backoff.TryAcquire());
        Assert.Equal(250, backoff.RecordFailure());
    }
}
