using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Models;
using DraftRescue.Application.Persistence;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Domain.Drafts;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4CoordinatorTests
{
    [Fact]
    public async Task StaleGenerationAndEmptySnapshotNeverProtectOrPersist()
    {
        var protector = new FakeProtector();
        var repository = new FakeRepository();
        using var coordinator = new PersistenceCheckpointCoordinator(protector, repository);
        var stale = Candidate(generation: 1, currentGeneration: 2, text: "stale");
        var empty = Candidate(generation: 1, currentGeneration: 1, text: string.Empty);

        Assert.Equal(CheckpointOutcome.SkippedStaleGeneration, (await coordinator.CheckpointAsync(stale, TestContext.Current.CancellationToken)).Outcome);
        Assert.Equal(CheckpointOutcome.SkippedEmpty, (await coordinator.CheckpointAsync(empty, TestContext.Current.CancellationToken)).Outcome);
        Assert.Equal(0, protector.Calls);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task SuccessfulCheckpointProtectsBeforeRepositoryAndCommits()
    {
        var protector = new FakeProtector();
        var repository = new FakeRepository();
        using var coordinator = new PersistenceCheckpointCoordinator(protector, repository);
        var result = await coordinator.CheckpointAsync(Candidate(), TestContext.Current.CancellationToken);

        Assert.Equal(CheckpointOutcome.Committed, result.Outcome);
        Assert.Equal(1, protector.Calls);
        Assert.Equal(1, repository.Calls);
        Assert.NotNull(repository.LastRecord);
        Assert.Equal(7, repository.LastRecord!.SnapshotSequence);
    }

    [Fact]
    public async Task ProtectionFailureNeverCallsRepository()
    {
        var protector = new FakeProtector { ThrowOnProtect = true };
        var repository = new FakeRepository();
        using var coordinator = new PersistenceCheckpointCoordinator(protector, repository);

        var result = await coordinator.CheckpointAsync(Candidate(), TestContext.Current.CancellationToken);

        Assert.Equal(CheckpointOutcome.ProtectionFailed, result.Outcome);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task RepositoryFailureLeavesTypedFailureAndNoPlaintextFallback()
    {
        var protector = new FakeProtector();
        var repository = new FakeRepository { ThrowOnUpsert = true };
        using var coordinator = new PersistenceCheckpointCoordinator(protector, repository);

        var result = await coordinator.CheckpointAsync(Candidate(), TestContext.Current.CancellationToken);

        Assert.Equal(CheckpointOutcome.RepositoryFailed, result.Outcome);
        Assert.Equal(1, protector.Calls);
        Assert.Equal(1, repository.Calls);
    }

    [Fact]
    public async Task CancellationDuringProtectionDiscardsCiphertextBeforeRepository()
    {
        using var cancellation = new CancellationTokenSource();
        var protector = new FakeProtector { OnProtect = cancellation.Cancel };
        var repository = new FakeRepository();
        using var coordinator = new PersistenceCheckpointCoordinator(protector, repository);

        var result = await coordinator.CheckpointAsync(Candidate(), cancellation.Token);

        Assert.Equal(CheckpointOutcome.Cancelled, result.Outcome);
        Assert.Equal(1, protector.Calls);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task StaleRepositoryResultDoesNotBecomeCheckpointCommit()
    {
        var repository = new FakeRepository { Result = ProtectedDraftUpsertResult.StaleIgnored };
        using var coordinator = new PersistenceCheckpointCoordinator(new FakeProtector(), repository);

        var result = await coordinator.CheckpointAsync(Candidate(), TestContext.Current.CancellationToken);

        Assert.Equal(CheckpointOutcome.StaleIgnored, result.Outcome);
    }

    [Fact]
    public async Task ConcurrentCheckpointsAreSerializedWithoutRetainingPlaintextQueue()
    {
        var protector = new FakeProtector { DelayMilliseconds = 10 };
        var repository = new FakeRepository();
        using var coordinator = new PersistenceCheckpointCoordinator(protector, repository);
        var token = TestContext.Current.CancellationToken;
        await Task.WhenAll(Enumerable.Range(1, 10).Select(sequence => coordinator.CheckpointAsync(Candidate((ulong)sequence), token)));

        Assert.Equal(10, protector.Calls);
        Assert.Equal(10, repository.Calls);
        Assert.Equal(1, protector.MaxConcurrentCalls);
    }

    private static CheckpointCandidate Candidate(ulong sequence = 7, ulong generation = 1, ulong currentGeneration = 1, string text = "checkpoint")
    {
        var snapshot = FieldTextSnapshot.Create(generation, sequence, Guid.NewGuid(), "test.profile", ReadStrategy.ValuePatternCertified,
            checked((long)sequence), DateTimeOffset.UtcNow, text);
        return CheckpointCandidate.Create(DraftId.New(), generation, currentGeneration, snapshot, "test.app", null, null,
            DraftPresentationKind.GenericText, 1, 1, new byte[] { 1, 2, 3 }, TimeSpan.FromMinutes(30));
    }

    private sealed class FakeProtector : IDraftProtector
    {
        private int _active;
        public int Calls;
        public int MaxConcurrentCalls;
        public bool ThrowOnProtect;
        public int DelayMilliseconds;
        public Action? OnProtect;

        public ProtectedDraftPayload Protect(DraftPlaintextPayload plaintext, DraftProtectionContext context)
        {
            Interlocked.Increment(ref Calls);
            var active = Interlocked.Increment(ref _active);
            MaxConcurrentCalls = Math.Max(MaxConcurrentCalls, active);
            try
            {
                if (ThrowOnProtect) throw new InvalidOperationException("synthetic protection failure");
                OnProtect?.Invoke();
                if (DelayMilliseconds > 0) Thread.Sleep(DelayMilliseconds);
                return ProtectedDraftPayload.Create(1, new byte[] { 9 });
            }
            finally { Interlocked.Decrement(ref _active); }
        }

        public DraftPlaintextPayload Unprotect(ProtectedDraftPayload protectedPayload, DraftProtectionContext context) =>
            throw new NotSupportedException();
    }

    private sealed class FakeRepository : IProtectedDraftRepository
    {
        public int Calls;
        public bool ThrowOnUpsert;
        public ProtectedDraftUpsertResult Result = ProtectedDraftUpsertResult.Inserted;
        public ProtectedDraftRecordV1? LastRecord;

        public Task<ProtectedDraftUpsertResult> UpsertAsync(ProtectedDraftRecordV1 record, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastRecord = record;
            if (ThrowOnUpsert) throw new InvalidOperationException("synthetic repository failure");
            return Task.FromResult(Result);
        }

        public Task<ProtectedDraftRecordV1?> GetProtectedAsync(DraftId draftId, CancellationToken cancellationToken = default) => Task.FromResult<ProtectedDraftRecordV1?>(null);
        public Task<IReadOnlyList<RecoverableDraftMetadata>> ListRecoverableMetadataAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RecoverableDraftMetadata>>(Array.Empty<RecoverableDraftMetadata>());
        public Task DeleteAsync(DraftId draftId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> DeleteExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task DeleteAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
