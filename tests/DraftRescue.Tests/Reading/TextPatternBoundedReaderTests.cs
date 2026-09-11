using System.Windows.Automation;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Models;
using DraftRescue.Application.Security;
using DraftRescue.Platform.Windows.Reading;
using Xunit;

namespace DraftRescue.Tests.Reading;

public sealed class TextPatternBoundedReaderTests
{
    [Fact]
    public async Task ClaimsBeforeRead_PreservesExactTextAndUsesLimitPlusOne()
    {
        var accessor = new FakeAccessor("  first\r\nsecond\u00A0  ");
        var claimer = new FakeClaimer { OnClaim = () => accessor.ClaimObserved = true };
        var reader = CreateReader(claimer, accessor);

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 64), CancellationToken.None);

        var success = Assert.IsType<EligibleTextReadResult.Success>(result);
        Assert.Equal("  first\r\nsecond\u00A0  ", success.Snapshot.Text);
        Assert.Equal(65, accessor.LastMaximumLength);
        Assert.True(accessor.ClaimObserved);
        Assert.Equal(nameof(FieldTextSnapshot), success.Snapshot.ToString());
    }

    [Fact]
    public async Task OversizedText_IsTypedTooLargeAndDoesNotProduceSnapshot()
    {
        var accessor = new FakeAccessor(new string('x', 11));
        var reader = CreateReader(new FakeClaimer(), accessor);

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 10), CancellationToken.None);

        var tooLarge = Assert.IsType<EligibleTextReadResult.TooLarge>(result);
        Assert.Equal(11, tooLarge.ObservedAtLeast);
        Assert.Equal(10, tooLarge.ConfiguredLimit);
    }

    [Fact]
    public async Task EmptyProviderResult_IsEmptySnapshotNotFailure()
    {
        var reader = CreateReader(new FakeClaimer(), new FakeAccessor(string.Empty));

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 16), CancellationToken.None);

        var empty = Assert.IsType<EligibleTextReadResult.Empty>(result);
        Assert.True(empty.Snapshot.IsEmpty);
    }

    [Fact]
    public async Task TargetChangeAfterProviderRead_DiscardsLateResult()
    {
        var resolver = new FakeResolver { Current = false };
        var reader = CreateReader(new FakeClaimer(), new FakeAccessor("safe"), resolver);

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 16), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.TargetChanged>(result);
    }

    [Theory]
    [InlineData(CapabilityClaimResult.Expired, typeof(EligibleTextReadResult.Expired))]
    [InlineData(CapabilityClaimResult.AlreadyConsumed, typeof(EligibleTextReadResult.AlreadyConsumed))]
    [InlineData(CapabilityClaimResult.Revoked, typeof(EligibleTextReadResult.RevokedOrStale))]
    [InlineData(CapabilityClaimResult.StaleContext, typeof(EligibleTextReadResult.RevokedOrStale))]
    [InlineData(CapabilityClaimResult.BindingUnavailable, typeof(EligibleTextReadResult.RevokedOrStale))]
    public async Task NonClaimedCapability_NeverCallsProvider(CapabilityClaimResult claim, Type expectedType)
    {
        var accessor = new FakeAccessor("must-not-read");
        var reader = CreateReader(new FakeClaimer { Result = claim }, accessor);

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 16), CancellationToken.None);

        Assert.IsType(expectedType, result);
        Assert.Equal(0, accessor.ReadCalls);
    }

    [Fact]
    public async Task UnsupportedStrategyConsumesCapabilityAndStopsBeforeProvider()
    {
        var accessor = new FakeAccessor("must-not-read");
        var claimer = new FakeClaimer();
        var reader = CreateReader(claimer, accessor);

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 16), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.UnsupportedReadStrategy>(result);
        Assert.Equal(1, claimer.Calls);
        Assert.Equal(0, accessor.ReadCalls);
    }

    [Fact]
    public async Task ProviderFailureContainsOnlyStableCode()
    {
        var accessor = new FakeAccessor { Failure = new InvalidOperationException("canary-provider-text") };
        var reader = CreateReader(new FakeClaimer(), accessor);

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 16), CancellationToken.None);

        var failure = Assert.IsType<EligibleTextReadResult.ProviderFailure>(result);
        Assert.Equal("DR-UIA-3001", failure.StableErrorCode);
        Assert.DoesNotContain("canary-provider-text", failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TimeoutSpendsCapabilityAndReturnsNoPartialText()
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var accessor = new FakeAccessor(completion.Task);
        var reader = CreateReader(new FakeClaimer(), accessor);

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 16, TimeSpan.FromMilliseconds(10)), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.Timeout>(result);
        completion.TrySetResult("late");
    }

    [Fact]
    public async Task CancellationReturnsCancelledWithoutSnapshot()
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = CreateReader(new FakeClaimer(), new FakeAccessor(completion.Task));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 16), cancellation.Token);

        Assert.IsType<EligibleTextReadResult.Cancelled>(result);
        completion.TrySetResult("late");
    }

    private static TextPatternBoundedReader CreateReader(
        FakeClaimer claimer,
        FakeAccessor accessor,
        FakeResolver? resolver = null) =>
        new(claimer, resolver ?? new FakeResolver(), accessor, new FakeClock());

    private static AllowedFieldHandle Handle()
    {
        var evidence = new DraftRescue.Application.Security.SecurityEvidenceSet(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            1, 1,
            ProfileResolutionStatus.Resolved,
            "draftrescue.synthetic", 1,
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
            DraftRescue.Application.Models.UiaControlKind.Document,
            DraftRescue.Application.Models.UiaFrameworkKind.Wpf);
        var registry = new TestRegistry(evidence);
        var decision = new SecurityPolicyEvaluator().Evaluate(evidence);
        return Assert.IsType<CapabilityIssueResult.Issued>(
            new AllowedFieldHandleIssuer(registry, new FakeClock()).Issue(evidence, decision)).Handle;
    }

    private sealed class FakeClaimer : IReadCapabilityClaimer
    {
        public CapabilityClaimResult Result { get; set; } = CapabilityClaimResult.Claimed;
        public int Calls { get; private set; }
        public Action? OnClaim { get; set; }
        public CapabilityClaimResult TryClaim(AllowedFieldHandle handle)
        {
            Calls++;
            OnClaim?.Invoke();
            return Result;
        }
    }

    private sealed class FakeResolver : ICertifiedReadTargetResolver
    {
        public bool Current { get; set; } = true;
        public bool Resolve { get; set; } = true;
        public bool TryResolve(AllowedFieldHandle handle, out CertifiedReadTarget target)
        {
            target = new CertifiedReadTarget(
                null!,
                1, 9, "draftrescue.synthetic", true);
            return Resolve;
        }
        public bool IsCurrent(CertifiedReadTarget target) => Current;
    }

    private sealed class FakeAccessor : ITextPatternDocumentAccessor
    {
        private readonly Task<string> _result;
        public FakeAccessor(string text) => _result = Task.FromResult(text);
        public FakeAccessor(Task<string> result) => _result = result;
        public FakeAccessor() => _result = Task.FromResult(string.Empty);
        public Exception? Failure { get; set; }
        public int LastMaximumLength { get; private set; }
        public int ReadCalls { get; private set; }
        public bool ClaimObserved { get; set; }
        public Task<string> ReadDocumentAsync(AutomationElement element, int maximumLength, CancellationToken cancellationToken)
        {
            ReadCalls++;
            LastMaximumLength = maximumLength;
            if (Failure is not null) return Task.FromException<string>(Failure);
            return _result;
        }
    }

    private sealed class FakeClock : IMonotonicClock
    {
        public long GetTimestampMilliseconds() => 123;
    }

    private sealed class TestRegistry(SecurityEvidenceSet evidence) : ICapabilityBindingRegistry
    {
        public bool TryGetCurrent(Guid bindingId, out CapabilityBindingSnapshot binding)
        {
            binding = new CapabilityBindingSnapshot(evidence.CandidateId, bindingId, evidence.ContextGeneration, evidence.ProfileId!, evidence.ProfileRevision, bindingId == evidence.BindingId);
            return binding.IsCurrent;
        }
    }
}
