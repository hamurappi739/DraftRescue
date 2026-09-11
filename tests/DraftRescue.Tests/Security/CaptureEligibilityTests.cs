using DraftRescue.Application.Security;
using Xunit;

namespace DraftRescue.Tests.Security;

public sealed class CaptureEligibilityTests
{
    [Fact]
    public void Allowed_IsTheOnlyDecisionThatMayPersist()
    {
        Assert.True(CaptureEligibility.Allowed().MayPersist);
    }

    [Theory]
    [MemberData(nameof(DeniedCases))]
    public void UnsafeOrUncertainContexts_FailClosed(CaptureEligibility eligibility)
    {
        Assert.False(eligibility.MayPersist);
        Assert.NotEqual(CaptureDenialReason.None, eligibility.DenialReason);
    }

    public static TheoryData<CaptureEligibility> DeniedCases => new()
    {
        CaptureEligibility.SecureField(),
        CaptureEligibility.CredentialLikeField(),
        CaptureEligibility.BankingOrFinancialField(),
        CaptureEligibility.PrivateBrowsing(),
        CaptureEligibility.Unsupported(),
        CaptureEligibility.Uncertain()
    };
}
