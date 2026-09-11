namespace DraftRescue.Application.Models;

/// <summary>
/// Fail-closed state of the metadata-only observation plane.
/// </summary>
public enum ObservationContextState
{
    NoContext = 0,
    CandidateMetadata = 1,
    Unsupported = 2,
    Transient = 3,
    IgnoredOwnProcess = 4
}
