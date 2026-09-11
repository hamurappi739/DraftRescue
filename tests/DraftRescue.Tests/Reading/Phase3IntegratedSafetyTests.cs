using System.Text.Json;
using System.Windows.Automation;
using DraftRescue.Application.Contracts.Drafts;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Drafts;
using DraftRescue.Application.Models;
using DraftRescue.Application.Security;
using DraftRescue.Platform.Windows.Reading;
using Xunit;

namespace DraftRescue.Tests.Reading;

public sealed class Phase3IntegratedSafetyTests
{
    [Fact]
    public async Task TextPatternSuccessFlowsOnlyToOneCurrentTrackerRecord()
    {
        const string canary = "phase3-text-canary";
        var reader = new TextPatternBoundedReader(
            new Claimer(), new TextResolver(), new TextAccessor(canary), new Clock(10));
        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 128), CancellationToken.None);
        var success = Assert.IsType<EligibleTextReadResult.Success>(result);
        var tracker = new InMemoryDraftTracker(new Clock(20));
        var context = Context();

        Assert.Equal(DraftApplyResult.Created, tracker.ApplySnapshot(context, success.Snapshot, 1));
        var current = Assert.IsType<DraftRecord>(tracker.GetCurrent(context));
        Assert.Equal(canary, current.CurrentSnapshot!.Text);
        Assert.DoesNotContain(canary, current.CurrentSnapshot.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(canary, JsonSerializer.Serialize(current.CurrentSnapshot), StringComparison.Ordinal);
        Assert.DoesNotContain("Revision", JsonSerializer.Serialize(current), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValuePatternSuccessFlowsOnlyToOneCurrentTrackerRecord()
    {
        const string canary = "phase3-value-canary";
        var reader = new ValuePatternBoundedReader(
            new Claimer(), new ValueResolver(), new ValueAccessor(canary), new Clock(10));
        var result = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 128), CancellationToken.None);
        var success = Assert.IsType<EligibleTextReadResult.Success>(result);
        var tracker = new InMemoryDraftTracker(new Clock(20));
        var context = Context();

        Assert.Equal(DraftApplyResult.Created, tracker.ApplySnapshot(context, success.Snapshot, 1));
        var current = Assert.IsType<DraftRecord>(tracker.GetCurrent(context));
        Assert.Equal(ReadStrategy.ValuePatternCertified, current.CurrentSnapshot!.ReadStrategy);
        Assert.Equal(canary, current.CurrentSnapshot.Text);
        Assert.DoesNotContain(canary, current.CurrentSnapshot.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ReadStrategy.TextPatternDocument)]
    [InlineData(ReadStrategy.ValuePatternCertified)]
    public async Task OversizedReadNeverMutatesTracker(ReadStrategy strategy)
    {
        var text = new string('x', 129);
        EligibleTextReadResult result = strategy == ReadStrategy.TextPatternDocument
            ? await new TextPatternBoundedReader(new Claimer(), new TextResolver(), new TextAccessor(text), new Clock(10))
                .ReadOnceAsync(Handle(), new ReadBudget(strategy, 128), CancellationToken.None)
            : await new ValuePatternBoundedReader(new Claimer(), new ValueResolver(), new ValueAccessor(text), new Clock(10))
                .ReadOnceAsync(Handle(), new ReadBudget(strategy, 128), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.TooLarge>(result);
        Assert.Null(new InMemoryDraftTracker(new Clock(20)).GetCurrent(Context()));
    }

    [Fact]
    public async Task FailedTimeoutAndCancellationPathsHaveNoTrackerMutation()
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tracker = new InMemoryDraftTracker(new Clock(20));
        var reader = new ValuePatternBoundedReader(new Claimer(), new ValueResolver(), new ValueAccessor(completion.Task), new Clock(10));
        var timeout = await reader.ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.ValuePatternCertified, 128, TimeSpan.FromMilliseconds(10)), CancellationToken.None);
        Assert.IsType<EligibleTextReadResult.Timeout>(timeout);
        Assert.Null(tracker.GetCurrent(Context()));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = await new TextPatternBoundedReader(new Claimer(), new TextResolver(), new TextAccessor(completion.Task), new Clock(10))
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 128), cancellation.Token);
        Assert.IsType<EligibleTextReadResult.Cancelled>(cancelled);
        Assert.Null(tracker.GetCurrent(Context()));
        completion.TrySetResult("late-provider-value");
    }

    [Fact]
    public async Task CapabilityFailureProducesNoProviderOrTrackerSideEffects()
    {
        var claimer = new Claimer { Result = CapabilityClaimResult.Revoked };
        var accessor = new TextAccessor("must-not-read");
        var result = await new TextPatternBoundedReader(claimer, new TextResolver(), accessor, new Clock(10))
            .ReadOnceAsync(Handle(), new ReadBudget(ReadStrategy.TextPatternDocument, 128), CancellationToken.None);

        Assert.IsType<EligibleTextReadResult.RevokedOrStale>(result);
        Assert.Equal(0, accessor.ReadCalls);
        Assert.Null(new InMemoryDraftTracker(new Clock(20)).GetCurrent(Context()));
    }

    [Fact]
    public void StaleReadCannotOverwriteNewerCurrentState()
    {
        var tracker = new InMemoryDraftTracker(new Clock(500));
        var context = Context();
        var newer = Snapshot(2, "newer");
        var older = Snapshot(1, "older");

        Assert.Equal(DraftApplyResult.Created, tracker.ApplySnapshot(context, newer, 1));
        Assert.Equal(DraftApplyResult.StaleIgnored, tracker.ApplySnapshot(context, older, 1));
        Assert.Equal("newer", tracker.GetCurrent(context)!.CurrentSnapshot!.Text);
    }

    private static DraftContextKey Context() => new(
        new DraftRescue.Domain.Context.ContextFingerprint("window"),
        new DraftRescue.Domain.Context.ContextFingerprint("field"));

    private static FieldTextSnapshot Snapshot(ulong sequence, string text) =>
        FieldTextSnapshot.Create(1, sequence, Guid.NewGuid(), "draftrescue.synthetic", ReadStrategy.TextPatternDocument, (long)sequence, DateTimeOffset.UtcNow, text);

    private static AllowedFieldHandle Handle()
    {
        var evidence = new SecurityEvidenceSet(
            Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"),
            1, 1, ProfileResolutionStatus.Resolved, "draftrescue.synthetic", 1,
            TargetVersionStatus.Certified, IntegrityBoundaryStatus.Accessible, PrivateModeStatus.NotApplicable,
            SecuritySignal<bool>.Known(false), SecuritySignal<bool>.Known(true), SecuritySignal<bool>.Known(false),
            ProviderHealthStatus.Healthy, SensitivePurpose.None, PositiveAllowPredicateStatus.Satisfied,
            BindingStatus.Current, UiaControlKind.Edit, UiaFrameworkKind.Wpf);
        var decision = new SecurityPolicyEvaluator().Evaluate(evidence);
        return Assert.IsType<CapabilityIssueResult.Issued>(
            new AllowedFieldHandleIssuer(new Registry(evidence), new Clock(0)).Issue(evidence, decision)).Handle;
    }

    private sealed class Claimer : IReadCapabilityClaimer
    {
        public CapabilityClaimResult Result { get; init; } = CapabilityClaimResult.Claimed;
        public CapabilityClaimResult TryClaim(AllowedFieldHandle handle) => Result;
    }

    private sealed class TextResolver : ICertifiedReadTargetResolver
    {
        public bool TryResolve(AllowedFieldHandle handle, out CertifiedReadTarget target)
        {
            target = new CertifiedReadTarget(null!, 1, 1, "draftrescue.synthetic", true);
            return true;
        }
        public bool IsCurrent(CertifiedReadTarget target) => true;
    }

    private sealed class ValueResolver : ICertifiedValueReadTargetResolver
    {
        public bool TryResolve(AllowedFieldHandle handle, out CertifiedValueReadTarget target)
        {
            target = new CertifiedValueReadTarget(null!, 1, 1, "draftrescue.synthetic", true);
            return true;
        }
        public bool IsCurrent(CertifiedValueReadTarget target) => true;
    }

    private sealed class TextAccessor : ITextPatternDocumentAccessor
    {
        private readonly Task<string> _result;
        public TextAccessor(string text) => _result = Task.FromResult(text);
        public TextAccessor(Task<string> result) => _result = result;
        public int ReadCalls { get; private set; }
        public Task<string> ReadDocumentAsync(AutomationElement element, int maximumLength, CancellationToken cancellationToken) { ReadCalls++; return _result; }
    }

    private sealed class ValueAccessor : IValuePatternAccessor
    {
        private readonly Task<string> _result;
        public ValueAccessor(string text) => _result = Task.FromResult(text);
        public ValueAccessor(Task<string> result) => _result = result;
        public Task<string> ReadValueAsync(AutomationElement element, CancellationToken cancellationToken) => _result;
    }

    private sealed class Clock(long now) : IMonotonicClock
    {
        public long GetTimestampMilliseconds() => now;
    }

    private sealed class Registry(SecurityEvidenceSet evidence) : ICapabilityBindingRegistry
    {
        public bool TryGetCurrent(Guid bindingId, out CapabilityBindingSnapshot binding)
        {
            binding = new CapabilityBindingSnapshot(evidence.CandidateId, bindingId, evidence.ContextGeneration, evidence.ProfileId!, evidence.ProfileRevision, bindingId == evidence.BindingId);
            return binding.IsCurrent;
        }
    }
}
