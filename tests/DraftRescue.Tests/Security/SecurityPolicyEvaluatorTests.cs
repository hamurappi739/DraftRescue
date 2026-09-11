using DraftRescue.Application.Models;
using DraftRescue.Application.Security;
using Xunit;

namespace DraftRescue.Tests.Security;

public sealed class SecurityPolicyEvaluatorTests
{
    private readonly SecurityPolicyEvaluator _evaluator = new();

    [Fact]
    public void SyntheticOrdinaryField_IsAllowedOnlyWhenEverySignalIsKnown()
    {
        var result = _evaluator.Evaluate(SafeEvidence());

        var allowed = Assert.IsType<SecurityDecision.Allowed>(result);
        Assert.Equal(SafeCandidateId, allowed.CandidateId);
        Assert.Equal("draftrescue.synthetic", allowed.ProfileId);
    }

    [Theory]
    [InlineData(SecuritySignalState.Unknown)]
    [InlineData(SecuritySignalState.Unavailable)]
    [InlineData(SecuritySignalState.Failed)]
    public void RequiredProtectedSignal_UncertaintyDenies(SecuritySignalState state)
    {
        var evidence = SafeEvidence() with
        {
            ProtectedContent = state == SecuritySignalState.Failed
                ? SecuritySignal<bool>.Failed("DR-OBS-1005")
                : state == SecuritySignalState.Unknown
                    ? SecuritySignal<bool>.Unknown()
                    : SecuritySignal<bool>.Unavailable()
        };

        AssertDenied(_evaluator.Evaluate(evidence), CaptureDenialReason.UncertainSecureState, "DR-SEC-2005");
    }

    [Fact]
    public void ProtectedTrue_DeniesBeforePositiveHints()
    {
        var evidence = SafeEvidence() with
        {
            ProtectedContent = SecuritySignal<bool>.Known(true),
            SensitivePurpose = SensitivePurpose.Credential
        };

        AssertDenied(_evaluator.Evaluate(evidence), CaptureDenialReason.SecureField, "DR-SEC-2001");
    }

    [Theory]
    [InlineData(ProfileResolutionStatus.Missing)]
    [InlineData(ProfileResolutionStatus.Invalid)]
    [InlineData(ProfileResolutionStatus.Ambiguous)]
    [InlineData(ProfileResolutionStatus.IncompatibleVersion)]
    public void UnresolvedProfile_Denies(ProfileResolutionStatus status)
    {
        AssertDenied(_evaluator.Evaluate(SafeEvidence() with { ProfileResolution = status }),
            CaptureDenialReason.UnsupportedProfile, "DR-SEC-2007");
    }

    [Theory]
    [InlineData(TargetVersionStatus.Unknown)]
    [InlineData(TargetVersionStatus.Incompatible)]
    public void UnknownOrIncompatibleTargetVersion_Denies(TargetVersionStatus status)
    {
        AssertDenied(_evaluator.Evaluate(SafeEvidence() with { TargetVersionStatus = status }),
            CaptureDenialReason.UnsupportedProfile, "DR-SEC-2007");
    }

    [Theory]
    [InlineData(PrivateModeStatus.ConfirmedPrivate, CaptureDenialReason.PrivateBrowsing, "DR-SEC-2004")]
    [InlineData(PrivateModeStatus.Unknown, CaptureDenialReason.UncertainPrivateMode, "DR-SEC-2005")]
    public void PrivateMode_Denies(PrivateModeStatus mode, CaptureDenialReason reason, string code)
    {
        AssertDenied(_evaluator.Evaluate(SafeEvidence() with { PrivateMode = mode }), reason, code);
    }

    [Theory]
    [InlineData(SensitivePurpose.Password, CaptureDenialReason.SecureField, "DR-SEC-2001")]
    [InlineData(SensitivePurpose.Credential, CaptureDenialReason.CredentialLikeField, "DR-SEC-2002")]
    [InlineData(SensitivePurpose.PinOrOtp, CaptureDenialReason.CredentialLikeField, "DR-SEC-2002")]
    [InlineData(SensitivePurpose.Payment, CaptureDenialReason.BankingOrFinancialField, "DR-SEC-2003")]
    [InlineData(SensitivePurpose.Banking, CaptureDenialReason.BankingOrFinancialField, "DR-SEC-2003")]
    [InlineData(SensitivePurpose.Unknown, CaptureDenialReason.UncertainSecureState, "DR-SEC-2005")]
    public void SensitivePurpose_Denies(SensitivePurpose purpose, CaptureDenialReason reason, string code)
    {
        AssertDenied(_evaluator.Evaluate(SafeEvidence() with { SensitivePurpose = purpose }), reason, code);
    }

    [Theory]
    [InlineData(ProviderHealthStatus.Stale, CaptureDenialReason.StaleContext, "DR-OBS-1003")]
    [InlineData(ProviderHealthStatus.Timeout, CaptureDenialReason.ProviderUnavailable, "DR-OBS-1004")]
    [InlineData(ProviderHealthStatus.AccessDenied, CaptureDenialReason.ProviderUnavailable, "DR-OBS-1005")]
    [InlineData(ProviderHealthStatus.Failed, CaptureDenialReason.ProviderUnavailable, "DR-OBS-1005")]
    public void ProviderHealth_Denies(ProviderHealthStatus health, CaptureDenialReason reason, string code)
    {
        AssertDenied(_evaluator.Evaluate(SafeEvidence() with { ProviderHealth = health }), reason, code);
    }

    [Fact]
    public void EditableOrReadOnlyUnknown_DeniesFailClosed()
    {
        AssertDenied(_evaluator.Evaluate(SafeEvidence() with { Editable = SecuritySignal<bool>.Unknown() }),
            CaptureDenialReason.UncertainSecureState, "DR-SEC-2005");
        AssertDenied(_evaluator.Evaluate(SafeEvidence() with { ReadOnly = SecuritySignal<bool>.Known(true) }),
            CaptureDenialReason.UncertainSecureState, "DR-SEC-2005");
    }

    [Fact]
    public void EditableTrueAndPasswordFalseAlone_CannotAllow()
    {
        var evidence = SafeEvidence() with
        {
            PositiveAllowPredicate = PositiveAllowPredicateStatus.NotSatisfied
        };

        AssertDenied(_evaluator.Evaluate(evidence), CaptureDenialReason.InsufficientPositiveEvidence, "DR-SEC-2007");
    }

    [Fact]
    public void HardDenyPrecedence_IsIndependentOfPositiveEvidence()
    {
        var evidence = SafeEvidence() with
        {
            ProtectedContent = SecuritySignal<bool>.Known(true),
            PositiveAllowPredicate = PositiveAllowPredicateStatus.Satisfied,
            SensitivePurpose = SensitivePurpose.None
        };

        var result = _evaluator.Evaluate(evidence);
        AssertDenied(result, CaptureDenialReason.SecureField, "DR-SEC-2001");
    }

    private static readonly Guid SafeCandidateId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SafeBindingId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    internal static SecurityEvidenceSet SafeEvidence() => new(
        SafeCandidateId,
        SafeBindingId,
        7,
        42,
        ProfileResolutionStatus.Resolved,
        "draftrescue.synthetic",
        1,
        TargetVersionStatus.Certified,
        IntegrityBoundaryStatus.Accessible,
        PrivateModeStatus.NotApplicable,
        SecuritySignal<bool>.Known(false),
        SecuritySignal<bool>.Known(true),
        SecuritySignal<bool>.Known(false),
        ProviderHealthStatus.Healthy,
        SensitivePurpose.None,
        PositiveAllowPredicateStatus.Satisfied,
        BindingStatus.Current,
        UiaControlKind.Edit,
        UiaFrameworkKind.Wpf);

    private static void AssertDenied(SecurityDecision decision, CaptureDenialReason reason, string code)
    {
        var denied = Assert.IsType<SecurityDecision.Denied>(decision);
        Assert.Equal(reason, denied.Reason);
        Assert.Equal(code, denied.StableErrorCode);
    }
}
