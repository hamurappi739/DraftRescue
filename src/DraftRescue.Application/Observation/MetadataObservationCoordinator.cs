using System.Threading.Channels;
using DraftRescue.Application.Contracts.Observation;
using DraftRescue.Application.Models;

namespace DraftRescue.Application.Observation;

/// <summary>
/// Coalesces foreground and focused metadata into a single fail-closed stream.
/// It never reads target content and never retains platform UIA objects.
/// </summary>
public sealed class MetadataObservationCoordinator : IObservationCoordinator
{
    private readonly IForegroundContextSource _foregroundSource;
    private readonly IFocusedElementMetadataSource _focusedSource;
    private readonly uint _ownProcessId;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Channel<SourceObservation> _sourceEvents =
        Channel.CreateBounded<SourceObservation>(new BoundedChannelOptions(128)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = false,
            SingleReader = true
        });
    private readonly Channel<ObservedContextChanged> _observations =
        Channel.CreateBounded<ObservedContextChanged>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = true,
            SingleReader = false
        });
    private readonly object _lifecycleGate = new();

    private Task? _pump;
    private int _producerCount;
    private int _disposed;
    private ulong _outputSequence;
    private ulong _contextGeneration;
    private ulong _lastForegroundSequence;
    private ulong _lastFocusedSequence;
    private ForegroundApplicationContextChanged? _foreground;
    private FocusedElementMetadataChanged? _focused;
    private bool _foregroundFaulted;
    private bool _focusedFaulted;
    private PublishedContext? _published;

    public MetadataObservationCoordinator(
        IForegroundContextSource foregroundSource,
        IFocusedElementMetadataSource focusedSource,
        uint ownProcessId)
    {
        _foregroundSource = foregroundSource;
        _focusedSource = focusedSource;
        _ownProcessId = ownProcessId;
    }

    public IAsyncEnumerable<ObservedContextChanged> WatchAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        EnsureStarted();
        return _observations.Reader.ReadAllAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _lifetime.Cancel();
        _sourceEvents.Writer.TryComplete();
        _observations.Writer.TryComplete();

        var foregroundDispose = _foregroundSource.DisposeAsync().AsTask();
        var focusedDispose = _focusedSource.DisposeAsync().AsTask();
        try
        {
            await Task.WhenAll(foregroundDispose, focusedDispose)
                .WaitAsync(TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Source disposal remains bounded; the pump is still cancelled below.
        }
        catch (Exception)
        {
            // A source cleanup failure cannot block cancellation of the coordinator.
        }

        var pump = _pump;
        if (pump is not null)
        {
            try
            {
                await pump.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (TimeoutException)
            {
            }
        }

        _lifetime.Dispose();
    }

    private void EnsureStarted()
    {
        lock (_lifecycleGate)
        {
            if (_pump is null)
            {
                _pump = Task.Run(PumpAsync, CancellationToken.None);
            }
        }
    }

    private async Task PumpAsync()
    {
        _producerCount = 2;
        var foregroundProducer = ConsumeForegroundAsync();
        var focusedProducer = ConsumeFocusedAsync();

        try
        {
            await foreach (var observation in _sourceEvents.Reader.ReadAllAsync(_lifetime.Token)
                               .ConfigureAwait(false))
            {
                Process(observation);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        finally
        {
            try
            {
                await Task.WhenAll(foregroundProducer, focusedProducer).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
            {
            }

            _observations.Writer.TryComplete();
        }
    }

    private async Task ConsumeForegroundAsync()
    {
        try
        {
            await foreach (var value in _foregroundSource.WatchAsync(_lifetime.Token)
                               .WithCancellation(_lifetime.Token).ConfigureAwait(false))
            {
                TryEnqueue(SourceObservation.FromForeground(value));
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            TryEnqueue(SourceObservation.Fault(SourceKind.Foreground));
        }
        finally
        {
            ProducerCompleted();
        }
    }

    private async Task ConsumeFocusedAsync()
    {
        try
        {
            await foreach (var value in _focusedSource.WatchAsync(_lifetime.Token)
                               .WithCancellation(_lifetime.Token).ConfigureAwait(false))
            {
                TryEnqueue(SourceObservation.FromFocused(value));
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            TryEnqueue(SourceObservation.Fault(SourceKind.Focused));
        }
        finally
        {
            ProducerCompleted();
        }
    }

    private void TryEnqueue(SourceObservation observation)
    {
        if (Volatile.Read(ref _disposed) == 0)
        {
            _sourceEvents.Writer.TryWrite(observation);
        }
    }

    private void ProducerCompleted()
    {
        if (Interlocked.Decrement(ref _producerCount) == 0)
        {
            _sourceEvents.Writer.TryComplete();
        }
    }

    private void Process(SourceObservation observation)
    {
        switch (observation.Kind)
        {
            case SourceKind.Foreground when observation.Foreground is not null:
                if (observation.Foreground.ObservationSequence <= _lastForegroundSequence)
                {
                    return;
                }

                _lastForegroundSequence = observation.Foreground.ObservationSequence;
                _foreground = observation.Foreground;
                _foregroundFaulted = false;
                if (_focusedSource is IFocusedElementMetadataReconciliation reconciliation)
                {
                    reconciliation.RequestMetadataReconciliation();
                }
                PublishIfChanged(observation.Foreground.ObservedAtMonotonicTimestamp);
                break;

            case SourceKind.Focused when observation.Focused is not null:
                if (observation.Focused.ObservationSequence <= _lastFocusedSequence)
                {
                    return;
                }

                _lastFocusedSequence = observation.Focused.ObservationSequence;
                _focused = observation.Focused;
                _focusedFaulted = false;
                PublishIfChanged(observation.Focused.ObservedAtMonotonicTimestamp);
                break;

            case SourceKind.ForegroundFault:
                _foregroundFaulted = true;
                PublishIfChanged(observation.ObservedAtMonotonicTimestamp);
                break;

            case SourceKind.FocusedFault:
                _focusedFaulted = true;
                PublishIfChanged(observation.ObservedAtMonotonicTimestamp);
                break;
        }
    }

    private void PublishIfChanged(long observedAtMonotonicTimestamp)
    {
        var state = ResolveState();
        var published = new PublishedContext(state, _foreground?.Identity, _focused?.Metadata);
        if (_published is not null && _published.Equals(published))
        {
            return;
        }

        _published = published;
        _contextGeneration++;
        _observations.Writer.TryWrite(new ObservedContextChanged(
            ++_outputSequence,
            _contextGeneration,
            observedAtMonotonicTimestamp,
            state,
            _foreground?.Identity,
            _focused?.Metadata));
    }

    private ObservationContextState ResolveState()
    {
        if (_foregroundFaulted || _focusedFaulted)
        {
            return ObservationContextState.Transient;
        }

        if (_foreground?.Identity is not ForegroundApplicationIdentity foreground)
        {
            return ObservationContextState.NoContext;
        }

        if (IsOwnProcess(foreground.ProcessId))
        {
            return ObservationContextState.IgnoredOwnProcess;
        }

        var focused = _focused?.Metadata;
        if (focused is null)
        {
            return ObservationContextState.Transient;
        }

        if (IsOwnProcess(focused.ProcessId))
        {
            return ObservationContextState.IgnoredOwnProcess;
        }

        if (focused.IntegrityCompatibility == IntegrityCompatibility.Incompatible)
        {
            return ObservationContextState.Unsupported;
        }

        if (focused.ProcessId != foreground.ProcessId)
        {
            return ObservationContextState.Transient;
        }

        if (focused.ControlKind is not UiaControlKind.Edit and not UiaControlKind.Document)
        {
            return ObservationContextState.Unsupported;
        }

        return focused.IsEditable switch
        {
            UiaBooleanSignal.True => ObservationContextState.CandidateMetadata,
            UiaBooleanSignal.False => ObservationContextState.Unsupported,
            _ => ObservationContextState.Transient
        };
    }

    private bool IsOwnProcess(int processId) => processId > 0 && (uint)processId == _ownProcessId;

    private enum SourceKind
    {
        Foreground,
        Focused,
        ForegroundFault,
        FocusedFault
    }

    private readonly record struct SourceObservation(
        SourceKind Kind,
        ForegroundApplicationContextChanged? Foreground,
        FocusedElementMetadataChanged? Focused,
        long ObservedAtMonotonicTimestamp)
    {
        internal static SourceObservation FromForeground(ForegroundApplicationContextChanged value) =>
            new(SourceKind.Foreground, value, null, value.ObservedAtMonotonicTimestamp);

        internal static SourceObservation FromFocused(FocusedElementMetadataChanged value) =>
            new(SourceKind.Focused, null, value, value.ObservedAtMonotonicTimestamp);

        internal static SourceObservation Fault(SourceKind source) =>
            new(source == SourceKind.Foreground ? SourceKind.ForegroundFault : SourceKind.FocusedFault,
                null,
                null,
                System.Diagnostics.Stopwatch.GetTimestamp());
    }

    private readonly record struct PublishedContext(
        ObservationContextState State,
        ForegroundApplicationIdentity? Foreground,
        FocusedElementMetadata? Focused);
}
