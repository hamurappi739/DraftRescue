namespace DraftRescue.Application.Contracts.Observation;

/// <summary>
/// Requests a bounded metadata-only focused-element reconciliation.
/// Implementations must enqueue work and return without provider traversal.
/// </summary>
public interface IFocusedElementMetadataReconciliation
{
    void RequestMetadataReconciliation();
}
