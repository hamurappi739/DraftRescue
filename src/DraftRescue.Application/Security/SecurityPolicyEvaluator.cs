namespace DraftRescue.Application.Security;

/// <summary>
/// Pure deterministic Phase-2 policy. It has no platform, storage or reader dependency.
/// </summary>
public sealed class SecurityPolicyEvaluator
{
    public SecurityDecision Evaluate(SecurityEvidenceSet evidence)
    {
        if (evidence.CandidateId == Guid.Empty || evidence.BindingId == Guid.Empty ||
            evidence.IntegrityBoundary != IntegrityBoundaryStatus.Accessible)
        {
            return Denied(CaptureDenialReason.UnsupportedContext, "DR-SEC-2006");
        }

        if (evidence.ProfileResolution != ProfileResolutionStatus.Resolved ||
            string.IsNullOrWhiteSpace(evidence.ProfileId) || evidence.ProfileRevision <= 0 ||
            (evidence.TargetVersionStatus != TargetVersionStatus.Certified &&
             evidence.TargetVersionStatus != TargetVersionStatus.NotApplicable))
        {
            return Denied(CaptureDenialReason.UnsupportedProfile, "DR-SEC-2007");
        }

        if (evidence.PrivateMode == PrivateModeStatus.ConfirmedPrivate)
        {
            return Denied(CaptureDenialReason.PrivateBrowsing, "DR-SEC-2004");
        }

        if (evidence.PrivateMode == PrivateModeStatus.Unknown)
        {
            return Denied(CaptureDenialReason.UncertainPrivateMode, "DR-SEC-2005");
        }

        if (evidence.ProtectedContent.State != SecuritySignalState.Known)
        {
            return Denied(CaptureDenialReason.UncertainSecureState, "DR-SEC-2005");
        }

        if (evidence.ProtectedContent.Value)
        {
            return Denied(CaptureDenialReason.SecureField, "DR-SEC-2001");
        }

        switch (evidence.SensitivePurpose)
        {
            case SensitivePurpose.Password:
                return Denied(CaptureDenialReason.SecureField, "DR-SEC-2001");
            case SensitivePurpose.Credential:
            case SensitivePurpose.PinOrOtp:
                return Denied(CaptureDenialReason.CredentialLikeField, "DR-SEC-2002");
            case SensitivePurpose.Payment:
            case SensitivePurpose.Banking:
                return Denied(CaptureDenialReason.BankingOrFinancialField, "DR-SEC-2003");
            case SensitivePurpose.Unknown:
                return Denied(CaptureDenialReason.UncertainSecureState, "DR-SEC-2005");
        }

        if (evidence.ProviderHealth != ProviderHealthStatus.Healthy)
        {
            return evidence.ProviderHealth == ProviderHealthStatus.Stale
                ? Denied(CaptureDenialReason.StaleContext, "DR-OBS-1003")
                : Denied(CaptureDenialReason.ProviderUnavailable,
                    evidence.ProviderHealth == ProviderHealthStatus.Timeout ? "DR-OBS-1004" : "DR-OBS-1005");
        }

        if (evidence.BindingStatus != BindingStatus.Current)
        {
            return Denied(CaptureDenialReason.StaleContext, "DR-OBS-1003");
        }

        if (!IsKnownTrue(evidence.Editable) || !IsKnownFalse(evidence.ReadOnly))
        {
            return Denied(CaptureDenialReason.UncertainSecureState, "DR-SEC-2005");
        }

        if (evidence.PositiveAllowPredicate != PositiveAllowPredicateStatus.Satisfied)
        {
            return Denied(CaptureDenialReason.InsufficientPositiveEvidence, "DR-SEC-2007");
        }

        return new SecurityDecision.Allowed(
            evidence.CandidateId,
            evidence.BindingId,
            evidence.ContextGeneration,
            evidence.ProfileId!,
            evidence.ProfileRevision);
    }

    private static bool IsKnownTrue(SecuritySignal<bool> signal) =>
        signal.State == SecuritySignalState.Known && signal.Value;

    private static bool IsKnownFalse(SecuritySignal<bool> signal) =>
        signal.State == SecuritySignalState.Known && !signal.Value;

    private static SecurityDecision.Denied Denied(CaptureDenialReason reason, string code) =>
        new(reason, code);
}
