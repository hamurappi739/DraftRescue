namespace DraftRescue.Application.Models;

/// <summary>
/// A content-free foreground observation emitted by a platform source.
/// </summary>
public sealed record ForegroundApplicationContextChanged(
    ulong ObservationSequence,
    long ObservedAtMonotonicTimestamp,
    ForegroundApplicationIdentity? Identity);
