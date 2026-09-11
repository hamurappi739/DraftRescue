using System.Diagnostics;

namespace DraftRescue.Platform.Windows.Reliability;

/// <summary>
/// Monotonic, content-free backoff state for unreliable accessibility providers.
/// The class owns no threads and stores only timing/failure counters.
/// </summary>
internal sealed class ProviderFailureBackoff
{
    internal const int MinimumDelayMilliseconds = 250;
    internal const int MaximumDelayMilliseconds = 5000;

    private readonly Func<long> _timestampProvider;
    private long _nextAttemptTimestamp;
    private int _consecutiveFailures;
    private long _suppressedCount;

    internal ProviderFailureBackoff(Func<long>? timestampProvider = null)
    {
        _timestampProvider = timestampProvider ?? Stopwatch.GetTimestamp;
    }

    internal long SuppressedCount => Interlocked.Read(ref _suppressedCount);

    internal int RemainingMilliseconds
    {
        get
        {
            var nextAttempt = Interlocked.Read(ref _nextAttemptTimestamp);
            if (nextAttempt <= 0)
            {
                return 0;
            }

            var remainingTicks = nextAttempt - _timestampProvider();
            if (remainingTicks <= 0)
            {
                Interlocked.Exchange(ref _nextAttemptTimestamp, 0);
                return 0;
            }

            var remainingMilliseconds = (int)Math.Ceiling(remainingTicks * 1000d / Stopwatch.Frequency);
            return Math.Clamp(remainingMilliseconds, 1, MaximumDelayMilliseconds);
        }
    }

    internal bool TryAcquire()
    {
        if (RemainingMilliseconds <= 0)
        {
            return true;
        }

        Interlocked.Increment(ref _suppressedCount);
        return false;
    }

    internal int RecordFailure()
    {
        var failureStreak = Interlocked.Increment(ref _consecutiveFailures);
        var delayMilliseconds = Math.Min(
            MaximumDelayMilliseconds,
            MinimumDelayMilliseconds * Math.Pow(2, Math.Min(failureStreak - 1, 5)));
        var delayTicks = (long)(delayMilliseconds * Stopwatch.Frequency / 1000d);
        Interlocked.Exchange(ref _nextAttemptTimestamp, _timestampProvider() + delayTicks);
        return (int)delayMilliseconds;
    }

    internal void RecordSuccess()
    {
        Interlocked.Exchange(ref _consecutiveFailures, 0);
        Interlocked.Exchange(ref _nextAttemptTimestamp, 0);
    }
}
