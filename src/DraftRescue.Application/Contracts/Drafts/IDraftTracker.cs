using DraftRescue.Application.Models;

namespace DraftRescue.Application.Contracts.Drafts;

/// <summary>
/// Current-state in-memory tracker. It deliberately has no persistence or
/// revision-history methods.
/// </summary>
public interface IDraftTracker
{
    DraftApplyResult ApplySnapshot(
        DraftContextKey context,
        FieldTextSnapshot snapshot,
        ulong currentContextGeneration);

    DraftRecord? GetCurrent(DraftContextKey context);
}
