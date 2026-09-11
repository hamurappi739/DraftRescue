using DraftRescue.Domain.Context;
using DraftRescue.Domain.Drafts;

namespace DraftRescue.Application.Models;

public readonly record struct DraftContextKey(
    ContextFingerprint Window,
    ContextFingerprint Field);

public enum EmptyCandidateState
{
    None = 0,
    Started = 1,
    Confirmed = 2
}

public enum DraftApplyResult
{
    Created = 0,
    Updated = 1,
    Unchanged = 2,
    StaleIgnored = 3,
    IdentityMismatch = 4,
    EmptyCandidateStarted = 5,
    EmptyCandidateConfirmed = 6
}

/// <summary>
/// One current state for one logical field. No revision collection is exposed.
/// </summary>
public sealed record DraftRecord(
    DraftId DraftId,
    DraftContextKey Context,
    ulong ContextGeneration,
    ulong LatestAcceptedSnapshotSequence,
    FieldTextSnapshot? CurrentSnapshot,
    DateTimeOffset UpdatedAtUtc,
    EmptyCandidateState EmptyCandidate,
    long? EmptyCandidateStartedAtMonotonic);
