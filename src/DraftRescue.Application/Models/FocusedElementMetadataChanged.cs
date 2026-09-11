namespace DraftRescue.Application.Models;

public sealed record FocusedElementMetadataChanged(
    ulong ObservationSequence,
    long ObservedAtMonotonicTimestamp,
    FocusedElementMetadata? Metadata);
