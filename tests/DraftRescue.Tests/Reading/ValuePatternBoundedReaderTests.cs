using System.Windows.Automation;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Models;
using DraftRescue.Application.Security;
using DraftRescue.Platform.Windows.Reading;
using Xunit;

namespace DraftRescue.Tests.Reading;

public sealed class ValuePatternBoundedReaderTests
{
    [Fact]
    public async Task ClaimsBeforeRead_PreservesExactValueAndStampsStrategy()
    {
        var accessor = new FakeAccessor("  value\r\nsecond\u00A0  ");
        var claimer = new FakeClaimer { OnClaim = () => accessor.ClaimObserved = true };
        var reader = CreateReader(claimer, accessor);

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 64), CancellationToken.None);

        var success = Assert.IsType<EligibleTextReadResult.Success>(result);
        Assert.Equal("  value\r\nsecond\u00A0  ", success.Snapshot.Text);
        Assert.Equal(ReadStrategy.ValuePatternCertified, success.Snapshot.ReadStrategy);
        Assert.True(accessor.ClaimObserved);
    }

    [Fact]
    public async Task OversizedValue_IsTypedTooLargeAndDoesNotProduceSnapshot()
    {
        var reader = CreateReader(new FakeClaimer(), new FakeAccessor(new string('x', 11)));

        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 10), CancellationToken.None);

        var tooLarge = Assert.IsType<EligibleTextReadResult.TooLarge>(result);
        Assert.Equal(11, tooLarge.ObservedAtLeast);
        Assert.Equal(10, tooLarge.ConfiguredLimit);
    }

    [Fact]
    public async Task EmptyValue_IsEmptySnapshotNotFailure()
    {
        var result = await CreateReader(new FakeClaimer(), new FakeAccessor(string.Empty))
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 16), CancellationToken.None);

        var empty = Assert.IsType<EligibleTextReadResult.Empty>(result);
        Assert.True(empty.Snapshot.IsEmpty);
    }

    [Fact]
    public async Task TargetChangeAfterProviderRead_DiscardsValue()
    {
        var resolver = new FakeResolver { Current = false };
        var result = await CreateReader(new FakeClaimer(), new FakeAccessor("safe"), resolver)
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 16), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.TargetChanged>(result);
    }

    [Fact]
    public async Task UnsupportedStrategyConsumesCapabilityAndStopsBeforeProvider()
    {
        var accessor = new FakeAccessor("must-not-read");
        var claimer = new FakeClaimer();
        var result = await CreateReader(claimer, accessor)
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 16), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.UnsupportedReadStrategy>(result);
        Assert.Equal(1, claimer.Calls);
        Assert.Equal(0, accessor.ReadCalls);
    }

    [Fact]
    public async Task NonClaimedCapabilityNeverCallsProvider()
    {
        var accessor = new FakeAccessor("must-not-read");
        var result = await CreateReader(new FakeClaimer { Result = CapabilityClaimResult.Expired }, accessor)
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 16), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.Expired>(result);
        Assert.Equal(0, accessor.ReadCalls);
    }

    [Fact]
    public async Task ProviderFailureContainsOnlyStableCode()
    {
        var accessor = new FakeAccessor { Failure = new InvalidOperationException("canary-provider-value") };
        var result = await CreateReader(new FakeClaimer(), accessor)
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 16), CancellationToken.None);

        var failure = Assert.IsType<EligibleTextReadResult.ProviderFailure>(result);
        Assert.Equal("DR-UIA-3002", failure.StableErrorCode);
        Assert.DoesNotContain("canary-provider-value", failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TimeoutDiscardsLateValue()
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = await CreateReader(new FakeClaimer(), new FakeAccessor(completion.Task))
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 16, TimeSpan.FromMilliseconds(10)), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.Timeout>(result);
        completion.TrySetResult("late");
    }

    [Fact]
    public async Task CancellationReturnsCancelledWithoutSnapshot()
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await CreateReader(new FakeClaimer(), new FakeAccessor(completion.Task))
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 16), cancellation.Token);

        Assert.IsType<EligibleTextReadResult.Cancelled>(result);
        completion.TrySetResult("late");
    }

    [Fact]
    public async Task SynchronousUnavailableProviderIsTypedUnsupported()
    {
        var result = await CreateReader(new FakeClaimer(), new FakeAccessor { ThrowUnavailableSynchronously = true })
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 16), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.UnsupportedReadStrategy>(result);
    }

    private static ValuePatternBoundedReader CreateReader(
        FakeClaimer claimer,
        FakeAccessor accessor,
        FakeResolver? resolver = null) =>
        new(claimer, resolver ?? new FakeResolver(), accessor, new FakeClock());

    private static AllowedFieldHandle Handle()
    {
        var evidence = new SecurityEvidenceSet(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            1, 1, ProfileResolutionStatus.Resolved, "draftrescue.synthetic", 1,
            TargetVersionStatus.Certified, IntegrityBoundaryStatus.Accessible,
            PrivateModeStatus.NotApplicable, SecuritySignal<bool>.Known(false),
            SecuritySignal<bool>.Known(true), SecuritySignal<bool>.Known(false),
            ProviderHealthStatus.Healthy, SensitivePurpose.None,
            PositiveAllowPredicateStatus.Satisfied, BindingStatus.Current,
            UiaControlKind.Edit, UiaFrameworkKind.Wpf);
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
        public CapabilityClaimResult TryClaim(AllowedFieldHandle handle) { Calls++; OnClaim?.Invoke(); return Result; }
    }

    private sealed class FakeResolver : ICertifiedValueReadTargetResolver
    {
        public bool Current { get; set; } = true;
        public bool Resolve { get; set; } = true;
        public bool TryResolve(AllowedFieldHandle handle, out CertifiedValueReadTarget target)
        {
            target = new CertifiedValueReadTarget(null!, 1, 9, "draftrescue.synthetic", true);
            return Resolve;
        }
        public bool IsCurrent(CertifiedValueReadTarget target) => Current;
    }

    private sealed class FakeAccessor : IValuePatternAccessor
    {
        private readonly Task<string> _result;
        public FakeAccessor(string value) => _result = Task.FromResult(value);
        public FakeAccessor(Task<string> result) => _result = result;
        public FakeAccessor() => _result = Task.FromResult(string.Empty);
        public Exception? Failure { get; set; }
        public bool ThrowUnavailableSynchronously { get; set; }
        public int ReadCalls { get; private set; }
        public bool ClaimObserved { get; set; }
        public Task<string> ReadValueAsync(AutomationElement element, CancellationToken cancellationToken)
        {
            ReadCalls++;
            if (ThrowUnavailableSynchronously) throw new ValuePatternUnavailableException();
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
