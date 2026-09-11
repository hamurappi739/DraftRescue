using System.Collections.Concurrent;
using DraftRescue.Application.Security;
using Xunit;

namespace DraftRescue.Tests.Security;

public sealed class AllowedFieldHandleTests
{
    [Fact]
    public void IssuerCreatesOpaqueHandleOnlyForAllowedDecision()
    {
        var evidence = SecurityPolicyEvaluatorTests.SafeEvidence();
        var registry = new FakeRegistry(evidence);
        var issuer = new AllowedFieldHandleIssuer(registry, new FakeClock());
        var result = issuer.Issue(evidence, new SecurityPolicyEvaluator().Evaluate(evidence));

        var issued = Assert.IsType<CapabilityIssueResult.Issued>(result);
        Assert.Equal(nameof(AllowedFieldHandle), issued.Handle.ToString());
        Assert.DoesNotContain(evidence.CandidateId.ToString(), issued.Handle.ToString(), StringComparison.Ordinal);
        Assert.Empty(typeof(AllowedFieldHandle).GetConstructors());
    }

    [Fact]
    public void DeniedDecisionCannotIssueCapability()
    {
        var evidence = SecurityPolicyEvaluatorTests.SafeEvidence() with
        {
            ProtectedContent = SecuritySignal<bool>.Known(true)
        };
        var issuer = new AllowedFieldHandleIssuer(new FakeRegistry(evidence), new FakeClock());
        var result = issuer.Issue(evidence, new SecurityPolicyEvaluator().Evaluate(evidence));

        var denied = Assert.IsType<CapabilityIssueResult.Denied>(result);
        Assert.Equal(CaptureDenialReason.UncertainSecureState, denied.Reason);
        Assert.Null(issuedHandle(result));
    }

    [Fact]
    public void IssueRechecksCurrentBindingAndGeneration()
    {
        var evidence = SecurityPolicyEvaluatorTests.SafeEvidence();
        var registry = new FakeRegistry(evidence) { IsCurrent = false };
        var issuer = new AllowedFieldHandleIssuer(registry, new FakeClock());

        var result = issuer.Issue(evidence, new SecurityPolicyEvaluator().Evaluate(evidence));

        var denied = Assert.IsType<CapabilityIssueResult.Denied>(result);
        Assert.Equal(CaptureDenialReason.StaleContext, denied.Reason);
        Assert.Equal("DR-OBS-1003", denied.StableErrorCode);
    }

    [Fact]
    public void ClaimIsSingleUseAndGenerationBound()
    {
        var evidence = SecurityPolicyEvaluatorTests.SafeEvidence();
        var clock = new FakeClock();
        var registry = new FakeRegistry(evidence);
        var issuer = new AllowedFieldHandleIssuer(registry, clock);
        var validator = new AllowedFieldHandleValidator(registry, clock);
        var handle = Assert.IsType<CapabilityIssueResult.Issued>(issuer.Issue(evidence, new SecurityPolicyEvaluator().Evaluate(evidence))).Handle;

        Assert.Equal(CapabilityClaimResult.Claimed, validator.TryClaim(handle, evidence));
        Assert.Equal(CapabilityClaimResult.AlreadyConsumed, validator.TryClaim(handle, evidence));
        Assert.Equal(CapabilityClaimResult.Invalid, validator.TryClaim(null, evidence));
    }

    [Fact]
    public void ExpiredCapabilityCannotBeClaimed()
    {
        var evidence = SecurityPolicyEvaluatorTests.SafeEvidence();
        var clock = new FakeClock();
        var registry = new FakeRegistry(evidence);
        var issuer = new AllowedFieldHandleIssuer(registry, clock, 1000);
        var validator = new AllowedFieldHandleValidator(registry, clock);
        var handle = Assert.IsType<CapabilityIssueResult.Issued>(issuer.Issue(evidence, new SecurityPolicyEvaluator().Evaluate(evidence))).Handle;

        clock.Now = 1000;
        Assert.Equal(CapabilityClaimResult.Expired, validator.TryClaim(handle, evidence));
        Assert.Equal(CapabilityClaimResult.Revoked, validator.TryClaim(handle, evidence));
    }

    [Fact]
    public void RevocationAndContextChangePreventClaim()
    {
        var evidence = SecurityPolicyEvaluatorTests.SafeEvidence();
        var clock = new FakeClock();
        var registry = new FakeRegistry(evidence);
        var issuer = new AllowedFieldHandleIssuer(registry, clock);
        var validator = new AllowedFieldHandleValidator(registry, clock);
        var handle = Assert.IsType<CapabilityIssueResult.Issued>(issuer.Issue(evidence, new SecurityPolicyEvaluator().Evaluate(evidence))).Handle;

        Assert.True(validator.Revoke(handle));
        Assert.Equal(CapabilityClaimResult.Revoked, validator.TryClaim(handle, evidence));

        var second = Assert.IsType<CapabilityIssueResult.Issued>(issuer.Issue(evidence, new SecurityPolicyEvaluator().Evaluate(evidence))).Handle;
        var changed = evidence with { ContextGeneration = evidence.ContextGeneration + 1 };
        Assert.Equal(CapabilityClaimResult.StaleContext, validator.TryClaim(second, changed));
    }

    [Fact]
    public void ConcurrentClaimsHaveExactlyOneWinner()
    {
        var evidence = SecurityPolicyEvaluatorTests.SafeEvidence();
        var clock = new FakeClock();
        var registry = new FakeRegistry(evidence);
        var issuer = new AllowedFieldHandleIssuer(registry, clock);
        var validator = new AllowedFieldHandleValidator(registry, clock);
        var handle = Assert.IsType<CapabilityIssueResult.Issued>(issuer.Issue(evidence, new SecurityPolicyEvaluator().Evaluate(evidence))).Handle;
        var results = new ConcurrentBag<CapabilityClaimResult>();

        Parallel.For(0, 32, _ => results.Add(validator.TryClaim(handle, evidence)));

        Assert.Equal(1, results.Count(x => x == CapabilityClaimResult.Claimed));
        Assert.Equal(31, results.Count(x => x == CapabilityClaimResult.AlreadyConsumed));
    }

    [Fact]
    public void HardMaximumAgeIsEnforced()
    {
        var evidence = SecurityPolicyEvaluatorTests.SafeEvidence();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AllowedFieldHandleIssuer(new FakeRegistry(evidence), new FakeClock(), 2001));
    }

    private static AllowedFieldHandle? issuedHandle(CapabilityIssueResult result) =>
        result is CapabilityIssueResult.Issued issued ? issued.Handle : null;

    private sealed class FakeClock : IMonotonicClock
    {
        public long Now { get; set; }
        public long GetTimestampMilliseconds() => Now;
    }

    private sealed class FakeRegistry(SecurityEvidenceSet evidence) : ICapabilityBindingRegistry
    {
        public bool IsCurrent { get; set; } = true;

        public bool TryGetCurrent(Guid bindingId, out CapabilityBindingSnapshot binding)
        {
            binding = new CapabilityBindingSnapshot(
                evidence.CandidateId, bindingId, evidence.ContextGeneration,
                evidence.ProfileId!, evidence.ProfileRevision, IsCurrent);
            return bindingId == evidence.BindingId;
        }
    }
}
