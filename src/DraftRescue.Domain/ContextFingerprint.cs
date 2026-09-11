namespace DraftRescue.Domain.Context;

/// <summary>
/// Opaque, non-content identifier used to correlate a window/field context.
/// The final fingerprint derivation algorithm belongs to a later phase.
/// </summary>
public readonly record struct ContextFingerprint(string Value)
{
    public override string ToString() => Value;
}
