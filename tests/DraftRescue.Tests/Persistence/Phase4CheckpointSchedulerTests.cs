using DraftRescue.Application.Models;
using DraftRescue.Application.Persistence;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Domain.Drafts;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4CheckpointSchedulerTests
{
    private static readonly CheckpointSchedulePolicy Policy = CheckpointSchedulePolicy.Create(
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(250),
        maxPendingDrafts: 2);

    [Fact]
    public void RapidSnapshotsCoalesceToNewestAndRespectTrailingDebounce()
    {
        using var scheduler = new PersistenceCheckpointScheduler(Policy);
        var first = Candidate(1, "one");
        var second = Candidate(2, "two", first.DraftId);

        Assert.Equal(CheckpointScheduleOutcome.Scheduled, scheduler.Schedule(first, 1_000).Outcome);
        Assert.Equal(CheckpointScheduleOutcome.Coalesced, scheduler.Schedule(second, 1_050).Outcome);
        Assert.False(scheduler.TryTakeDue(1_149, out _));
        Assert.True(scheduler.TryTakeDue(1_150, out var selected));
        Assert.Equal(2UL, selected!.Snapshot.SnapshotSequence);
    }

    [Fact]
    public void MaximumDirtyAgeForcesDueCandidateDuringContinuousChanges()
    {
        using var scheduler = new PersistenceCheckpointScheduler(Policy);
        var first = Candidate(1, "one");
        var second = Candidate(2, "two", first.DraftId);
        var third = Candidate(3, "three", first.DraftId);
        var fourth = Candidate(4, "four", first.DraftId);
        var fifth = Candidate(5, "five", first.DraftId);
        var sixth = Candidate(6, "six", first.DraftId);

        scheduler.Schedule(first, 0);
        scheduler.Schedule(second, 90);
        scheduler.Schedule(third, 180);
        scheduler.Schedule(fourth, 270);
        scheduler.Schedule(fifth, 360);
        scheduler.Schedule(sixth, 450);

        Assert.False(scheduler.TryTakeDue(499, out _));
        Assert.True(scheduler.TryTakeDue(500, out var selected));
        Assert.Equal(6UL, selected!.Snapshot.SnapshotSequence);
    }

    [Fact]
    public void CapacityExceededDoesNotRetainAdditionalPlaintextCandidate()
    {
        using var scheduler = new PersistenceCheckpointScheduler(Policy);
        Assert.Equal(CheckpointScheduleOutcome.Scheduled, scheduler.Schedule(Candidate(1, "one"), 0).Outcome);
        Assert.Equal(CheckpointScheduleOutcome.Scheduled, scheduler.Schedule(Candidate(1, "two"), 0).Outcome);
        var rejected = Candidate(1, "rejected");

        Assert.Equal(CheckpointScheduleOutcome.CapacityExceeded, scheduler.Schedule(rejected, 0).Outcome);
        Assert.Equal(2, scheduler.PendingCount);
    }

    [Fact]
    public void FailedCheckpointGetsOneDelayedRetryAndCompletionRemovesIt()
    {
        using var scheduler = new PersistenceCheckpointScheduler(Policy);
        var candidate = Candidate(1, "retry");
        scheduler.Schedule(candidate, 0);
        Assert.True(scheduler.TryTakeDue(100, out var selected));
        Assert.True(scheduler.Complete(selected!, CheckpointOutcome.RepositoryFailed, 100));
        Assert.False(scheduler.TryTakeDue(349, out _));
        Assert.True(scheduler.TryTakeDue(350, out selected));
        Assert.True(scheduler.Complete(selected!, CheckpointOutcome.Committed, 350));
        Assert.False(scheduler.TryTakeDue(1_000, out _));
    }

    [Fact]
    public void NewerPendingSnapshotSurvivesOlderInFlightCompletion()
    {
        using var scheduler = new PersistenceCheckpointScheduler(Policy);
        var first = Candidate(1, "one");
        var second = Candidate(2, "two", first.DraftId);
        scheduler.Schedule(first, 0);
        Assert.True(scheduler.TryTakeDue(100, out var selected));
        scheduler.Schedule(second, 101);

        Assert.True(scheduler.Complete(selected!, CheckpointOutcome.RepositoryFailed, 110));
        Assert.True(scheduler.TryTakeDue(201, out var newest));
        Assert.Equal(2UL, newest!.Snapshot.SnapshotSequence);
    }

    [Fact]
    public void StaleSequenceIsRejectedWithoutReplacingPendingCandidate()
    {
        using var scheduler = new PersistenceCheckpointScheduler(Policy);
        var current = Candidate(3, "current");
        var stale = Candidate(2, "stale", current.DraftId);
        scheduler.Schedule(current, 0);

        Assert.Equal(CheckpointScheduleOutcome.StaleIgnored, scheduler.Schedule(stale, 10).Outcome);
        Assert.True(scheduler.TryTakeDue(100, out var selected));
        Assert.Equal(3UL, selected!.Snapshot.SnapshotSequence);
    }

    private static CheckpointCandidate Candidate(ulong sequence, string text, DraftId? draftId = null)
    {
        var id = draftId ?? DraftId.New();
        var snapshot = FieldTextSnapshot.Create(
            contextGeneration: 1,
            snapshotSequence: sequence,
            captureAttemptId: Guid.NewGuid(),
            profileId: "test.profile",
            readStrategy: ReadStrategy.ValuePatternCertified,
            capturedAtMonotonic: checked((long)sequence),
            capturedAtUtc: DateTimeOffset.UnixEpoch.AddSeconds(sequence),
            text: text);
        return CheckpointCandidate.Create(
            id, 1, 1, snapshot, "test.app", null, null,
            DraftPresentationKind.GenericText, 1, 1, new byte[] { 1 }, TimeSpan.FromMinutes(30));
    }
}
