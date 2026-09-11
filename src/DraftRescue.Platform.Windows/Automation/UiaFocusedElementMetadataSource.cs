using System.Diagnostics;
using System.Threading.Channels;
using Uia = System.Windows.Automation;
using DraftRescue.Application.Contracts.Observation;
using DraftRescue.Application.Models;
using DraftRescue.Platform.Windows.Security;
using DraftRescue.Platform.Windows.Reliability;

namespace DraftRescue.Platform.Windows.Automation;

/// <summary>
/// Focus metadata source. UI Automation provider calls run on a dedicated MTA worker;
/// the focus callback only signals work and never traverses or reads content.
/// </summary>
public sealed class UiaFocusedElementMetadataSource : IFocusedElementMetadataSource, IFocusedElementMetadataReconciliation
{
    private const int StartupTimeoutMilliseconds = 5000;

    private readonly IUiaFocusApi _api;
    private readonly Channel<FocusedElementMetadataChanged> _observations =
        Channel.CreateBounded<FocusedElementMetadataChanged>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = true,
            SingleReader = false
        });
    private readonly ManualResetEventSlim _startupCompleted = new(false);
    private readonly ManualResetEventSlim _workAvailable = new(false);
    private readonly object _lifecycleGate = new();
    private readonly ProviderFailureBackoff _backoff = new();

    private Thread? _worker;
    private Exception? _startupFailure;
    private int _pending;
    private ulong _sequence;
    private int _disposed;
    private long _reconciliationSignalCount;
    private long _metadataReadCount;
    private long _metadataNonNullCount;
    private long _metadataFailureCount;
    private int _lastMetadataProcessId;

    /// <summary>Content-free counters for bounded experiment diagnostics.</summary>
    public long ReconciliationSignalCount => Interlocked.Read(ref _reconciliationSignalCount);

    public long MetadataReadCount => Interlocked.Read(ref _metadataReadCount);

    public long MetadataNonNullCount => Interlocked.Read(ref _metadataNonNullCount);

    /// <summary>Count of provider/read failures, without retaining exception details or target content.</summary>
    public long MetadataFailureCount => Interlocked.Read(ref _metadataFailureCount);

    /// <summary>Number of focus signals ignored while a provider failure backoff was active.</summary>
    public long BackoffSuppressedCount => _backoff.SuppressedCount;

    public int LastMetadataProcessId => Volatile.Read(ref _lastMetadataProcessId);

    public UiaFocusedElementMetadataSource()
        : this(new ManagedUiaFocusApi())
    {
    }

    internal UiaFocusedElementMetadataSource(IUiaFocusApi api)
    {
        _api = api;
    }

    public IAsyncEnumerable<FocusedElementMetadataChanged> WatchAsync(
        CancellationToken cancellationToken)
    {
        EnsureStarted();
        return _observations.Reader.ReadAllAsync(cancellationToken);
    }

    public void RequestMetadataReconciliation()
    {
        SignalMetadataWork();
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        _workAvailable.Set();
        _observations.Writer.TryComplete();
        var worker = _worker;
        if (worker is not null && worker.IsAlive && !ReferenceEquals(worker, Thread.CurrentThread))
        {
            worker.Join(StartupTimeoutMilliseconds);
        }

        _startupCompleted.Dispose();
        _workAvailable.Dispose();
        return ValueTask.CompletedTask;
    }

    private void EnsureStarted()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        lock (_lifecycleGate)
        {
            if (_worker is null)
            {
                _worker = new Thread(RunWorker)
                {
                    IsBackground = true,
                    Name = "DraftRescue.UIA.FocusMetadata"
                };
                _worker.SetApartmentState(ApartmentState.MTA);
                _worker.Start();
            }
        }

        if (!_startupCompleted.Wait(StartupTimeoutMilliseconds))
        {
            throw new InvalidOperationException("DR-OBS-0011");
        }

        if (_startupFailure is not null)
        {
            throw new InvalidOperationException("DR-OBS-0012");
        }
    }

    private void RunWorker()
    {
        try
        {
            _api.RegisterFocusChanged(OnFocusChanged);
        }
        catch (Exception)
        {
            _startupFailure = new InvalidOperationException("DR-OBS-0013");
            _startupCompleted.Set();
            _observations.Writer.TryComplete();
            return;
        }

        _startupCompleted.Set();
        SignalMetadataWork();
        try
        {
            while (Volatile.Read(ref _disposed) == 0)
            {
                _workAvailable.Wait();
                _workAvailable.Reset();
                if (Volatile.Read(ref _disposed) != 0)
                {
                    break;
                }

                if (Interlocked.Exchange(ref _pending, 0) == 0)
                {
                    continue;
                }

                if (!_backoff.TryAcquire())
                {
                    var backoffMilliseconds = _backoff.RemainingMilliseconds;
                    // This is the dedicated MTA worker, never the Avalonia UI thread.
                    // A bounded sleep prevents a repeated focus storm from becoming a
                    // hot provider retry loop while keeping disposal bounded by 5 seconds.
                    Thread.Sleep(Math.Max(1, backoffMilliseconds));
                    continue;
                }

                FocusedElementMetadata? metadata = null;
                try
                {
                    Interlocked.Increment(ref _metadataReadCount);
                    metadata = _api.ReadFocusedElementMetadata();
                    _backoff.RecordSuccess();
                    if (metadata is not null)
                    {
                        Interlocked.Increment(ref _metadataNonNullCount);
                        Volatile.Write(ref _lastMetadataProcessId, metadata.ProcessId);
                    }
                }
                catch (Exception)
                {
                    // Provider errors remain an unavailable metadata result.
                    Interlocked.Increment(ref _metadataFailureCount);
                    _backoff.RecordFailure();
                }

                _observations.Writer.TryWrite(new FocusedElementMetadataChanged(
                    ++_sequence,
                    Stopwatch.GetTimestamp(),
                    metadata));
            }
        }
        finally
        {
            try
            {
                _api.UnregisterFocusChanged(OnFocusChanged);
            }
            catch (Exception)
            {
                // Disposal remains bounded; provider messages are not logged.
            }

            _observations.Writer.TryComplete();
        }
    }

    private void OnFocusChanged()
    {
        SignalMetadataWork();
    }

    private void SignalMetadataWork()
    {
        if (Volatile.Read(ref _disposed) == 0)
        {
            Interlocked.Increment(ref _reconciliationSignalCount);
            Interlocked.Exchange(ref _pending, 1);
            _workAvailable.Set();
        }
    }

}

internal interface IUiaFocusApi
{
    void RegisterFocusChanged(Action callback);

    void UnregisterFocusChanged(Action callback);

    FocusedElementMetadata? ReadFocusedElementMetadata();
}

internal sealed class ManagedUiaFocusApi : IUiaFocusApi
{
    private Uia.AutomationFocusChangedEventHandler? _handler;

    public void RegisterFocusChanged(Action callback)
    {
        _handler = (_, _) => callback();
        Uia.Automation.AddAutomationFocusChangedEventHandler(_handler);
    }

    public void UnregisterFocusChanged(Action callback)
    {
        if (_handler is not null)
        {
            Uia.Automation.RemoveAutomationFocusChangedEventHandler(_handler);
            _handler = null;
        }
    }

    public FocusedElementMetadata? ReadFocusedElementMetadata()
    {
        var request = new Uia.CacheRequest
        {
            TreeScope = Uia.TreeScope.Element
        };
        request.Add(Uia.AutomationElement.ProcessIdProperty);
        request.Add(Uia.AutomationElement.NativeWindowHandleProperty);
        request.Add(Uia.AutomationElement.ControlTypeProperty);
        request.Add(Uia.AutomationElement.FrameworkIdProperty);
        request.Add(Uia.AutomationElement.IsPasswordProperty);
        request.Add(Uia.AutomationElement.IsEnabledProperty);
        request.Add(Uia.AutomationElement.IsKeyboardFocusableProperty);
        request.Add(Uia.AutomationElement.HasKeyboardFocusProperty);
        request.Add(Uia.AutomationElement.IsOffscreenProperty);
        request.Add(Uia.AutomationElement.AutomationIdProperty);
        request.Add(Uia.AutomationElement.ClassNameProperty);
        request.Add(Uia.AutomationElement.IsValuePatternAvailableProperty);
        request.Add(Uia.AutomationElement.IsTextPatternAvailableProperty);
        request.Add(Uia.ValuePattern.Pattern);

        var focused = Uia.AutomationElement.FocusedElement;
        if (focused is null)
        {
            return null;
        }

        var cached = focused.GetUpdatedCache(request);
        var controlKind = MapControlKind(cached.Current.ControlType?.Id);
        var isReadOnly = ReadOnlySignal(cached);
        var isEditable = controlKind is UiaControlKind.Edit or UiaControlKind.Document
            ? isReadOnly switch
            {
                UiaBooleanSignal.True => UiaBooleanSignal.False,
                UiaBooleanSignal.False => UiaBooleanSignal.True,
                _ => UiaBooleanSignal.Unknown
            }
            : UiaBooleanSignal.False;
        var processId = cached.Current.ProcessId;

        var metadata = new FocusedElementMetadata(
            processId,
            cached.Current.NativeWindowHandle,
            controlKind,
            MapFramework(cached.Current.FrameworkId),
            isEditable,
            isReadOnly,
            ToSignal(cached.Current.IsPassword),
            ToSignal(cached.Current.IsEnabled),
            ToSignal(cached.Current.IsKeyboardFocusable),
            ToSignal(cached.Current.HasKeyboardFocus),
            ToSignal(cached.Current.IsOffscreen),
            NormalizeStructuralToken(cached.Current.AutomationId),
            NormalizeStructuralToken(cached.Current.ClassName),
            MapCapabilities(cached));
        return metadata with
        {
            IntegrityCompatibility = ProcessIntegrityCompatibilityReader.Compare(processId, Environment.ProcessId)
        };
    }

    private static UiaCapabilityHints MapCapabilities(Uia.AutomationElement element)
    {
        var hints = UiaCapabilityHints.None;
        if (GetCachedBoolean(element, Uia.AutomationElement.IsValuePatternAvailableProperty)) hints |= UiaCapabilityHints.ValuePatternAvailable;
        if (GetCachedBoolean(element, Uia.AutomationElement.IsTextPatternAvailableProperty)) hints |= UiaCapabilityHints.TextPatternAvailable;
        return hints;
    }

    private static bool GetCachedBoolean(Uia.AutomationElement element, Uia.AutomationProperty property)
    {
        try
        {
            return element.GetCachedPropertyValue(property) is bool value && value;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static UiaBooleanSignal ReadOnlySignal(Uia.AutomationElement element)
    {
        try
        {
            var pattern = (Uia.ValuePattern)element.GetCachedPattern(Uia.ValuePattern.Pattern);
            return ToSignal(pattern.Current.IsReadOnly);
        }
        catch (Exception)
        {
            return UiaBooleanSignal.Unknown;
        }
    }

    private static string? NormalizeStructuralToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length is 0 or > 64 || normalized.Any(char.IsControl) ? null : normalized;
    }

    private static UiaBooleanSignal ToSignal(bool value) => value ? UiaBooleanSignal.True : UiaBooleanSignal.False;

    private static UiaControlKind MapControlKind(int? id) => id switch
    {
        50004 => UiaControlKind.Edit,
        50030 => UiaControlKind.Document,
        50033 => UiaControlKind.Pane,
        50032 => UiaControlKind.Window,
        50000 => UiaControlKind.Button,
        50007 => UiaControlKind.Menu,
        50031 => UiaControlKind.Dialog,
        null => UiaControlKind.Unknown,
        _ => UiaControlKind.Other
    };

    private static UiaFrameworkKind MapFramework(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "win32" => UiaFrameworkKind.Win32,
        "wpf" => UiaFrameworkKind.Wpf,
        "winui" => UiaFrameworkKind.WinUi,
        "chrome" or "chromium" or "electron" => UiaFrameworkKind.Chromium,
        "avalonia" => UiaFrameworkKind.Avalonia,
        null or "" => UiaFrameworkKind.Unknown,
        _ => UiaFrameworkKind.Other
    };
}
