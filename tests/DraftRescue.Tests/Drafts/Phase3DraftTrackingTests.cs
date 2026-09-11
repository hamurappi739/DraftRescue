using System.Text.Json;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Drafts;
using DraftRescue.Application.Models;
using DraftRescue.Application.Security;
using DraftRescue.Domain.Context;
using Xunit;

namespace DraftRescue.Tests.Drafts;

public sealed class Phase3DraftTrackingTests
{
    private static readonly DraftContextKey Context = new(
        new ContextFingerprint("window-synthetic"),
        new ContextFingerprint("field-synthetic"));

    [Fact]
    public void SnapshotPreservesExactTextAndDoesNotSerializeCanary()
    {
        const string canary = "  line 1\r\nline 2\u00A0  ";
        var snapshot = Snapshot(canary, generation: 3, sequence: 1);

        Assert.Equal(canary, snapshot.Text);
        Assert.Equal(canary.Length, snapshot.TextLengthUtf16);
        Assert.False(snapshot.IsEmpty);
        Assert.Equal(nameof(FieldTextSnapshot), snapshot.ToString());

        var serialized = JsonSerializer.Serialize(snapshot);
        Assert.DoesNotContain(canary, serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void NonEmptyEditsReplaceCurrentStateWithoutRevisionHistory()
    {
        var clock = new FakeClock();
        var tracker = new InMemoryDraftTracker(clock);
        Assert.Equal(DraftApplyResult.Created, tracker.ApplySnapshot(Context, Snapshot("first", 7, 1), 7));
        Assert.Equal(DraftApplyResult.Updated, tracker.ApplySnapshot(Context, Snapshot("second", 7, 2), 7));
        Assert.Equal(DraftApplyResult.Unchanged, tracker.ApplySnapshot(Context, Snapshot("second", 7, 3), 7));

        var current = tracker.GetCurrent(Context);
        Assert.NotNull(current);
        Assert.Equal("second", current!.CurrentSnapshot!.Text);
        Assert.Equal(3UL, current.LatestAcceptedSnapshotSequence);
        Assert.DoesNotContain(typeof(DraftRecord).GetProperties(), property =>
            typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) &&
            property.PropertyType != typeof(string));
    }

    [Fact]
    public void OlderSequenceOrGenerationCannotOverwriteCurrentState()
    {
        var tracker = new InMemoryDraftTracker(new FakeClock());
        tracker.ApplySnapshot(Context, Snapshot("new", 8, 8), 8);

        Assert.Equal(DraftApplyResult.StaleIgnored, tracker.ApplySnapshot(Context, Snapshot("old", 8, 7), 8));
        Assert.Equal(DraftApplyResult.StaleIgnored, tracker.ApplySnapshot(Context, Snapshot("wrong-generation", 9, 9), 8));
        Assert.Equal("new", tracker.GetCurrent(Context)!.CurrentSnapshot!.Text);
        Assert.Equal(8UL, tracker.GetCurrent(Context)!.LatestAcceptedSnapshotSequence);
    }

    [Fact]
    public void EmptyReadRequiresSecondAuthorizedObservationAnd300Milliseconds()
    {
        var clock = new FakeClock { Now = 1000 };
        var tracker = new InMemoryDraftTracker(clock);
        tracker.ApplySnapshot(Context, Snapshot("recoverable", 1, 1), 1);

        clock.Now = 1100;
        Assert.Equal(DraftApplyResult.EmptyCandidateStarted, tracker.ApplySnapshot(Context, Snapshot(string.Empty, 1, 2), 1));
        Assert.Equal("recoverable", tracker.GetCurrent(Context)!.CurrentSnapshot!.Text);

        clock.Now = 1399;
        Assert.Equal(DraftApplyResult.EmptyCandidateStarted, tracker.ApplySnapshot(Context, Snapshot(string.Empty, 1, 3), 1));
        Assert.Equal("recoverable", tracker.GetCurrent(Context)!.CurrentSnapshot!.Text);

        clock.Now = 1400;
        Assert.Equal(DraftApplyResult.EmptyCandidateConfirmed, tracker.ApplySnapshot(Context, Snapshot(string.Empty, 1, 4), 1));
        Assert.True(tracker.GetCurrent(Context)!.CurrentSnapshot!.IsEmpty);
        Assert.Equal(EmptyCandidateState.Confirmed, tracker.GetCurrent(Context)!.EmptyCandidate);
    }

    [Fact]
    public void NonEmptyReadCancelsPendingEmptyCandidate()
    {
        var clock = new FakeClock { Now = 10 };
        var tracker = new InMemoryDraftTracker(clock);
        tracker.ApplySnapshot(Context, Snapshot("before-clear", 1, 1), 1);
        clock.Now = 20;
        tracker.ApplySnapshot(Context, Snapshot(string.Empty, 1, 2), 1);

        clock.Now = 30;
        Assert.Equal(DraftApplyResult.Updated, tracker.ApplySnapshot(Context, Snapshot("typed-again", 1, 3), 1));
        var current = tracker.GetCurrent(Context)!;
        Assert.Equal("typed-again", current.CurrentSnapshot!.Text);
        Assert.Equal(EmptyCandidateState.None, current.EmptyCandidate);
    }

    [Fact]
    public void ReadBudgetAndFailureResultsAreTypedAndContentFree()
    {
        var budget = new ReadBudget(ReadStrategy.TextPatternDocument, 1024, TimeSpan.FromSeconds(1));
        Assert.Equal(1024, budget.MaximumUtf16CodeUnits);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadBudget(ReadStrategy.ValuePatternCertified, 262_145));
        Assert.IsType<EligibleTextReadResult.TooLarge>(new EligibleTextReadResult.TooLarge(1025, 1024));
        var failure = new EligibleTextReadResult.ProviderFailure("DR-UIA-3001");
        Assert.Equal("DR-UIA-3001", failure.StableErrorCode);
        Assert.DoesNotContain("canary", failure.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static FieldTextSnapshot Snapshot(string text, ulong generation, ulong sequence) =>
        FieldTextSnapshot.Create(
            generation,
            sequence,
            Guid.NewGuid(),
            "draftrescue.synthetic",
            ReadStrategy.TextPatternDocument,
            (long)sequence,
            DateTimeOffset.UnixEpoch.AddMilliseconds(sequence),
            text);

    private sealed class FakeClock : IMonotonicClock
    {
        public long Now { get; set; }
        public long GetTimestampMilliseconds() => Now;
    }
}
