using System.Threading.Channels;
using DraftRescue.Application.Contracts.Observation;
using DraftRescue.Application.Models;
using DraftRescue.Application.Observation;
using DomainApplicationId = DraftRescue.Domain.Context.ApplicationId;
using Xunit;

namespace DraftRescue.Tests.Observation;

public sealed class MetadataObservationCoordinatorTests
{
    [Fact]
    public async Task SameLogicalContext_IsCoalesced_AndGenerationAdvancesOnChange()
    {
        var foreground = new FakeForegroundSource();
        var focused = new FakeFocusedSource();
        await using var coordinator = new MetadataObservationCoordinator(
            foreground,
            focused,
            (uint)Environment.ProcessId + 1);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = coordinator.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        foreground.Emit(Foreground(1, 42, 100));
        focused.Emit(Focused(1, 42, 100, UiaBooleanSignal.True));
        var candidate = await ReadUntilAsync(enumerator, ObservationContextState.CandidateMetadata, cancellationToken);
        Assert.True(candidate.ContextGeneration > 0);
        Assert.True(focused.ReconciliationRequestCount > 0);

        foreground.Emit(Foreground(2, 42, 100));
        focused.Emit(Focused(2, 42, 100, UiaBooleanSignal.True));

        focused.Emit(Focused(3, 42, 100, UiaBooleanSignal.False));
        var unsupported = await ReadUntilAsync(enumerator, ObservationContextState.Unsupported, cancellationToken);
        Assert.Equal(candidate.ContextGeneration + 1, unsupported.ContextGeneration);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task OlderSourceSequence_IsIgnored()
    {
        var foreground = new FakeForegroundSource();
        var focused = new FakeFocusedSource();
        await using var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = coordinator.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        foreground.Emit(Foreground(2, 42, 200));
        foreground.Emit(Foreground(1, 99, 900));
        focused.Emit(Focused(1, 42, 200, UiaBooleanSignal.True));

        var candidate = await ReadUntilAsync(enumerator, ObservationContextState.CandidateMetadata, cancellationToken);
        Assert.Equal(42, candidate.Foreground!.Value.ProcessId);
        Assert.Equal((nint)200, candidate.Foreground.Value.NativeWindowHandle);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task OwnProcess_IsIgnoredAndDoesNotBecomeCandidate()
    {
        var foreground = new FakeForegroundSource();
        var focused = new FakeFocusedSource();
        var ownProcessId = (uint)Environment.ProcessId;
        await using var coordinator = new MetadataObservationCoordinator(foreground, focused, ownProcessId);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = coordinator.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        foreground.Emit(Foreground(1, (int)ownProcessId, 100));
        focused.Emit(Focused(1, (int)ownProcessId, 100, UiaBooleanSignal.True));

        var ignored = await ReadUntilAsync(enumerator, ObservationContextState.IgnoredOwnProcess, cancellationToken);
        Assert.Equal((int)ownProcessId, ignored.Foreground!.Value.ProcessId);
        Assert.NotEqual(ObservationContextState.CandidateMetadata, ignored.State);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task IncompatibleIntegrity_IsUnsupportedBeforeCandidate()
    {
        var foreground = new FakeForegroundSource();
        var focused = new FakeFocusedSource();
        await using var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = coordinator.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        foreground.Emit(Foreground(1, 42, 100));
        focused.Emit(Focused(1, 42, 100, UiaBooleanSignal.True) with
        {
            Metadata = Focused(1, 42, 100, UiaBooleanSignal.True).Metadata! with
            {
                IntegrityCompatibility = IntegrityCompatibility.Incompatible
            }
        });

        var unsupported = await ReadUntilAsync(enumerator, ObservationContextState.Unsupported, cancellationToken);
        Assert.Equal(42, unsupported.Focused!.ProcessId);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task MismatchedFocusedProcess_IsTransient()
    {
        var foreground = new FakeForegroundSource();
        var focused = new FakeFocusedSource();
        await using var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = coordinator.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        foreground.Emit(Foreground(1, 42, 100));
        focused.Emit(Focused(1, 43, 200, UiaBooleanSignal.True));

        var transient = await ReadUntilAsync(
            enumerator,
            value => value.State == ObservationContextState.Transient && value.Focused?.ProcessId == 43,
            cancellationToken);
        Assert.Equal(42, transient.Foreground!.Value.ProcessId);
        Assert.Equal(43, transient.Focused!.ProcessId);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task FocusProviderFault_RemainsTransientUntilItsSourceRecovers()
    {
        var foreground = new FakeForegroundSource();
        var focused = new FakeFocusedSource();
        await using var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = coordinator.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        foreground.Emit(Foreground(1, 42, 100));
        focused.Emit(Focused(1, 42, 100, UiaBooleanSignal.True));
        _ = await ReadUntilAsync(enumerator, ObservationContextState.CandidateMetadata, cancellationToken);

        focused.Fail();
        var transient = await ReadUntilAsync(enumerator, ObservationContextState.Transient, cancellationToken);
        Assert.Equal(42, transient.Foreground!.Value.ProcessId);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_IsIdempotentAndDisposesBothSourcesOnce()
    {
        var foreground = new FakeForegroundSource();
        var focused = new FakeFocusedSource();
        var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);

        _ = coordinator.WatchAsync(TestContext.Current.CancellationToken);
        await coordinator.DisposeAsync();
        await coordinator.DisposeAsync();

        Assert.Equal(1, foreground.DisposeCount);
        Assert.Equal(1, focused.DisposeCount);
    }

    [Fact]
    public async Task Dispose_ReleasesSourceThatIgnoresCancellationWithinBound()
    {
        var foreground = new FakeForegroundSource();
        var focused = new HungFocusedSource();
        var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
        _ = coordinator.WatchAsync(TestContext.Current.CancellationToken);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await coordinator.DisposeAsync();
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Dispose took {stopwatch.Elapsed}.");
        Assert.Equal(1, focused.DisposeCount);
    }

    private static async Task<ObservedContextChanged> ReadUntilAsync(
        IAsyncEnumerator<ObservedContextChanged> enumerator,
        ObservationContextState state,
        CancellationToken cancellationToken)
        => await ReadUntilAsync(enumerator, value => value.State == state, cancellationToken);

    private static async Task<ObservedContextChanged> ReadUntilAsync(
        IAsyncEnumerator<ObservedContextChanged> enumerator,
        Func<ObservedContextChanged, bool> predicate,
        CancellationToken cancellationToken)
    {
        while (await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2), cancellationToken))
        {
            if (predicate(enumerator.Current))
            {
                return enumerator.Current;
            }
        }

        throw new Xunit.Sdk.XunitException("Expected observation was not emitted.");
    }

    private static ForegroundApplicationContextChanged Foreground(
        ulong sequence,
        int processId,
        nint hwnd) =>
        new(sequence, (long)sequence, new ForegroundApplicationIdentity(
            new DomainApplicationId("fixture"), processId, hwnd));

    private static FocusedElementMetadataChanged Focused(
        ulong sequence,
        int processId,
        nint hwnd,
        UiaBooleanSignal editable) =>
        new(sequence, (long)sequence, new FocusedElementMetadata(
            processId,
            hwnd,
            UiaControlKind.Edit,
            UiaFrameworkKind.Win32,
            editable,
            editable == UiaBooleanSignal.True ? UiaBooleanSignal.False : UiaBooleanSignal.True,
            UiaBooleanSignal.False,
            UiaBooleanSignal.True,
            UiaBooleanSignal.True,
            UiaBooleanSignal.True,
            UiaBooleanSignal.False,
            "fixture-id",
            "Edit",
            UiaCapabilityHints.ValuePatternAvailable));

    private sealed class FakeForegroundSource : IForegroundContextSource
    {
        private readonly Channel<ForegroundApplicationContextChanged> _events =
            Channel.CreateUnbounded<ForegroundApplicationContextChanged>();

        public IAsyncEnumerable<ForegroundApplicationContextChanged> WatchAsync(CancellationToken cancellationToken) =>
            _events.Reader.ReadAllAsync(cancellationToken);

        public int DisposeCount { get; private set; }

        public void Emit(ForegroundApplicationContextChanged value) => _events.Writer.TryWrite(value);

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            _events.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }

    }

    private sealed class FakeFocusedSource : IFocusedElementMetadataSource, IFocusedElementMetadataReconciliation
    {
        private readonly Channel<FocusedElementMetadataChanged> _events =
            Channel.CreateUnbounded<FocusedElementMetadataChanged>();

        public IAsyncEnumerable<FocusedElementMetadataChanged> WatchAsync(CancellationToken cancellationToken) =>
            _events.Reader.ReadAllAsync(cancellationToken);

        public int DisposeCount { get; private set; }

        public int ReconciliationRequestCount { get; private set; }

        public void Emit(FocusedElementMetadataChanged value) => _events.Writer.TryWrite(value);

        public void RequestMetadataReconciliation() => ReconciliationRequestCount++;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            _events.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }

        public void Fail() => _events.Writer.TryComplete(new InvalidOperationException("fixture"));
    }

    private sealed class HungFocusedSource : IFocusedElementMetadataSource
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DisposeCount { get; private set; }

        public async IAsyncEnumerable<FocusedElementMetadataChanged> WatchAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await _release.Task.ConfigureAwait(false);
            yield break;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            _release.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }
}
