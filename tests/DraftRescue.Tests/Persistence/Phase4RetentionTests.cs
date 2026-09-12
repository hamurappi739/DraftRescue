using DraftRescue.Application.Contracts.Retention;
using DraftRescue.Application.Retention;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4RetentionTests
{
    [Fact]
    public async Task StartupCleanupRetriesOnceAndExposesOnlySafeCounts()
    {
        var service = new FakeRetentionService { FailuresBeforeSuccess = 1, Result = 3 };
        await using var coordinator = new RetentionCleanupCoordinator(service, new FakeClock(), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1));

        var deleted = await coordinator.CleanupOnStartupAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, deleted);
        Assert.Equal(2, service.Calls);
        Assert.Equal(2, coordinator.Health.AttemptCount);
        Assert.Equal(1, coordinator.Health.FailureCount);
        Assert.Equal(3, coordinator.Health.LastDeletedCount);
    }

    [Fact]
    public async Task RuntimeCleanupIsLowFrequencyAndStopsWithoutError()
    {
        var service = new FakeRetentionService { Result = 1, CompletionAfterCalls = 2 };
        await using var coordinator = new RetentionCleanupCoordinator(service, new FakeClock(), TimeSpan.FromMilliseconds(20), TimeSpan.Zero);

        coordinator.Start(TestContext.Current.CancellationToken);
        await service.Completed.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await coordinator.DisposeAsync();

        Assert.True(service.Calls >= 2);
        Assert.Equal(0, coordinator.Health.FailureCount);
    }

    [Fact]
    public async Task CancelledRetryDoesNotContinueInBackground()
    {
        using var cancellation = new CancellationTokenSource();
        var service = new FakeRetentionService { FailuresBeforeSuccess = int.MaxValue, CancelAfterFirstCall = cancellation };
        await using var coordinator = new RetentionCleanupCoordinator(service, new FakeClock(), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.CleanupOnStartupAsync(cancellation.Token));
        Assert.Equal(1, service.Calls);
    }

    private sealed class FakeClock : IWallClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch.AddMinutes(10);
    }

    private sealed class FakeRetentionService : IRetentionService
    {
        public int FailuresBeforeSuccess;
        public int Result;
        public int CompletionAfterCalls;
        public CancellationTokenSource? CancelAfterFirstCall;
        public int Calls;
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<int> CleanupExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Calls == 1) CancelAfterFirstCall?.Cancel();
            if (Calls >= CompletionAfterCalls && CompletionAfterCalls > 0) Completed.TrySetResult();
            if (FailuresBeforeSuccess-- > 0) throw new InvalidOperationException("synthetic failure");
            return Task.FromResult(Result);
        }
    }
}
