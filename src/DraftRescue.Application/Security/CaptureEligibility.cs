namespace DraftRescue.Application.Security;

/// <summary>
/// Fail-closed decision controlling whether a future draft is allowed to cross
/// the persistence boundary. Unknown/uncertain context is never permission.
/// </summary>
public readonly record struct CaptureEligibility
{
    private CaptureEligibility(bool mayPersist, CaptureDenialReason reason, string? stableErrorCode)
    {
        MayPersist = mayPersist;
        DenialReason = reason;
        StableErrorCode = stableErrorCode;
    }

    public bool MayPersist { get; }

    public CaptureDenialReason DenialReason { get; }

    public string? StableErrorCode { get; }

    public static CaptureEligibility Allowed() => new(true, CaptureDenialReason.None, null);

    public static CaptureEligibility SecureField() => new(false, CaptureDenialReason.SecureField, "DR-SEC-2001");

    public static CaptureEligibility CredentialLikeField() => new(false, CaptureDenialReason.CredentialLikeField, "DR-SEC-2002");

    public static CaptureEligibility BankingOrFinancialField() => new(false, CaptureDenialReason.BankingOrFinancialField, "DR-SEC-2003");

    public static CaptureEligibility PrivateBrowsing() => new(false, CaptureDenialReason.PrivateBrowsing, "DR-SEC-2004");

    public static CaptureEligibility Unsupported() => new(false, CaptureDenialReason.UnsupportedContext, "DR-SEC-2006");

    public static CaptureEligibility Uncertain() => new(false, CaptureDenialReason.UncertainContext, "DR-SEC-2005");
}
