using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Models;
using DraftRescue.Application.Persistence;
using DraftRescue.Application.Security;
using DraftRescue.Domain.Drafts;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4CheckpointWorkerTests
{
    [Fact]
    public async Task ProcessDueDelegatesThroughCoordinatorAndClearsCommittedWork()
    {
        var clock = new FakeClock { Now = 1_000 };
        var scheduler = new PersistenceCheckpointScheduler(Policy());
        var protector = new FakeProtector();
        var repository = new FakeRepository();
        using var coordinator = new PersistenceCheckpointCoordinator(protector, repository);
        await using var worker = new PersistenceCheckpointWorker(scheduler, coordinator, clock);
        var candidate = Candidate(1, "durable");

        Assert.Equal(CheckpointScheduleOutcome.Scheduled, worker.Schedule(candidate).Outcome);
        Assert.Equal(0, await worker.ProcessDueAsync(TestContext.Current.CancellationToken));
        clock.Now = 1_100;
        Assert.Equal(1, await worker.ProcessDueAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, protector.Calls);
        Assert.Equal(1, repository.Calls);
        Assert.Equal(0, scheduler.PendingCount);
    }

    [Fact]
    public async Task ProcessDueLeavesDelayedRetryAfterRepositoryFailure()
    {
        var clock = new FakeClock { Now = 0 };
        var scheduler = new PersistenceCheckpointScheduler(Policy());
        var repository = new FakeRepository { ThrowOnUpsert = true };
        using var coordinator = new PersistenceCheckpointCoordinator(new FakeProtector(), repository);
        await using var worker = new PersistenceCheckpointWorker(scheduler, coordinator, clock);
        worker.Schedule(Candidate(1, "retry"));

        clock.Now = 100;
        Assert.Equal(1, await worker.ProcessDueAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, scheduler.PendingCount);
        clock.Now = 349;
        Assert.Equal(0, await worker.ProcessDueAsync(TestContext.Current.CancellationToken));
        clock.Now = 350;
        Assert.Equal(1, await worker.ProcessDueAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartRejectsDuplicateStartAndDisposeStopsWorker()
    {
        var scheduler = new PersistenceCheckpointScheduler(Policy());
        using var coordinator = new PersistenceCheckpointCoordinator(new FakeProtector(), new FakeRepository());
        await using var worker = new PersistenceCheckpointWorker(scheduler, coordinator, new FakeClock());

        worker.Start(TestContext.Current.CancellationToken);
        Assert.Throws<InvalidOperationException>(() => worker.Start(TestContext.Current.CancellationToken));
        Assert.True(worker.IsRunning);
        await worker.DisposeAsync();
        Assert.False(worker.IsRunning);
    }

    [Fact]
    public async Task BackgroundWorkerWakesForScheduleSignalAndCommitsDueCandidate()
    {
        var clock = new FakeClock { Now = 0 };
        var scheduler = new PersistenceCheckpointScheduler(Policy());
        var repository = new FakeRepository();
        using var coordinator = new PersistenceCheckpointCoordinator(new FakeProtector(), repository);
        await using var worker = new PersistenceCheckpointWorker(scheduler, coordinator, clock);
        worker.Start(TestContext.Current.CancellationToken);

        var candidate = Candidate(1, "background");
        Assert.Equal(CheckpointScheduleOutcome.Scheduled, worker.Schedule(candidate).Outcome);
        clock.Now = 100;
        // Re-submit the same sequence solely to wake the worker after the injected
        // clock advances; the scheduler must reject it without retaining a duplicate.
        Assert.Equal(CheckpointScheduleOutcome.StaleIgnored, worker.Schedule(candidate).Outcome);

        await repository.Committed.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, repository.Calls);
        Assert.Equal(0, scheduler.PendingCount);
    }

    private static CheckpointSchedulePolicy Policy() => CheckpointSchedulePolicy.Create(
        TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(250), 4);

    private static CheckpointCandidate Candidate(ulong sequence, string text)
    {
        var snapshot = FieldTextSnapshot.Create(1, sequence, Guid.NewGuid(), "test.profile",
            ReadStrategy.ValuePatternCertified, checked((long)sequence), DateTimeOffset.UnixEpoch, text);
        return CheckpointCandidate.Create(DraftId.New(), 1, 1, snapshot, "test.app", null, null,
            DraftPresentationKind.GenericText, 1, 1, new byte[] { 1 }, TimeSpan.FromMinutes(30));
    }

    private sealed class FakeClock : IMonotonicClock
    {
        public long Now;
        public long GetTimestampMilliseconds() => Volatile.Read(ref Now);
    }

    private sealed class FakeProtector : IDraftProtector
    {
        public int Calls;
        public ProtectedDraftPayload Protect(DraftPlaintextPayload plaintext, DraftProtectionContext context)
        {
            Interlocked.Increment(ref Calls);
            return ProtectedDraftPayload.Create(1, new byte[] { 9 });
        }

        public DraftPlaintextPayload Unprotect(ProtectedDraftPayload protectedPayload, DraftProtectionContext context) =>
            throw new NotSupportedException();
    }

    private sealed class FakeRepository : IProtectedDraftRepository
    {
        public int Calls;
        public bool ThrowOnUpsert;
        public TaskCompletionSource<bool> Committed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<ProtectedDraftUpsertResult> UpsertAsync(ProtectedDraftRecordV1 record, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            if (ThrowOnUpsert) throw new InvalidOperationException("synthetic repository failure");
            Committed.TrySetResult(true);
            return Task.FromResult(ProtectedDraftUpsertResult.Inserted);
        }

        public Task<ProtectedDraftRecordV1?> GetProtectedAsync(DraftId draftId, CancellationToken cancellationToken = default) => Task.FromResult<ProtectedDraftRecordV1?>(null);
        public Task<IReadOnlyList<RecoverableDraftMetadata>> ListRecoverableMetadataAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RecoverableDraftMetadata>>(Array.Empty<RecoverableDraftMetadata>());
        public Task DeleteAsync(DraftId draftId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> DeleteExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task DeleteAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
