namespace DraftRescue.Application.Models;

/// <summary>
/// Normalized, metadata-only context transition emitted by the observation coordinator.
/// </summary>
public sealed record ObservedContextChanged(
    ulong ObservationSequence,
    ulong ContextGeneration,
    long ObservedAtMonotonicTimestamp,
    ObservationContextState State,
    ForegroundApplicationIdentity? Foreground,
    FocusedElementMetadata? Focused);
