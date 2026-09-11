using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using DraftRescue.Application.Contracts.Observation;
using DraftRescue.Application.Models;
using DraftRescue.Domain.Context;

namespace DraftRescue.Platform.Windows.Observation;

/// <summary>
/// Content-free foreground-window source backed by an out-of-context WinEvent hook.
/// </summary>
public sealed class WinEventForegroundContextSource : IForegroundContextSource
{
    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutOfContext = 0x0000;
    private const uint WineventSkipOwnProcess = 0x0002;
    private const uint WmQuit = 0x0012;
    private const uint WmApp = 0x8000;
    private const uint ReconcileMessage = WmApp + 1;
    private const int StartupTimeoutMilliseconds = 5000;

    private readonly IWinEventApi _api;
    private readonly uint _ownProcessId;
    private readonly Channel<RawForegroundEnvelope> _rawEvents =
        Channel.CreateBounded<RawForegroundEnvelope>(new BoundedChannelOptions(128)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleWriter = true,
            SingleReader = true
        });
    private readonly Channel<ForegroundApplicationContextChanged> _observations =
        Channel.CreateBounded<ForegroundApplicationContextChanged>(new BoundedChannelOptions(128)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = true,
            SingleReader = false
        });
    private readonly ManualResetEventSlim _startupCompleted = new(false);
    private readonly object _lifecycleGate = new();

    private Thread? _worker;
    private WinEventCallback? _callback;
    private nint _hook;
    private uint _workerThreadId;
    private ulong _sequence;
    private Exception? _startupFailure;
    private int _disposed;

    public WinEventForegroundContextSource()
        : this(NativeWinEventApi.Instance, (uint)Environment.ProcessId)
    {
    }

    internal WinEventForegroundContextSource(IWinEventApi api, uint ownProcessId)
    {
        _api = api;
        _ownProcessId = ownProcessId;
    }

    public IAsyncEnumerable<ForegroundApplicationContextChanged> WatchAsync(
        CancellationToken cancellationToken)
    {
        EnsureStarted();
        return _observations.Reader.ReadAllAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        _rawEvents.Writer.TryComplete();
        _observations.Writer.TryComplete();

        var worker = _worker;
        if (worker is not null && worker.IsAlive)
        {
            _api.PostThreadMessage(_workerThreadId, WmQuit, nint.Zero, nint.Zero);
            if (!ReferenceEquals(Thread.CurrentThread, worker))
            {
                worker.Join(StartupTimeoutMilliseconds);
            }
        }

        _startupCompleted.Dispose();
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
                    Name = "DraftRescue.WinEvent.Foreground"
                };
                _worker.Start();
            }
        }

        if (!_startupCompleted.Wait(StartupTimeoutMilliseconds))
        {
            throw new InvalidOperationException("DR-OBS-0001");
        }

        if (_startupFailure is not null)
        {
            throw new InvalidOperationException("DR-OBS-0002");
        }
    }

    private void RunWorker()
    {
        try
        {
            _api.EnsureMessageQueue();
            _workerThreadId = _api.GetCurrentThreadId();
            _callback = OnWinEvent;
            _hook = _api.SetWinEventHook(
                EventSystemForeground,
                EventSystemForeground,
                WineventOutOfContext | WineventSkipOwnProcess,
                _callback);

            if (_hook == nint.Zero)
            {
                throw new InvalidOperationException("DR-OBS-0003");
            }
        }
        catch (Exception)
        {
            _startupFailure = new InvalidOperationException("DR-OBS-0004");
            _startupCompleted.Set();
            _observations.Writer.TryComplete();
            return;
        }

        _startupCompleted.Set();

        try
        {
            while (Volatile.Read(ref _disposed) == 0)
            {
                var result = _api.GetMessage(out var message);
                if (result <= 0 || message.Message == WmQuit)
                {
                    break;
                }

                if (message.Message == ReconcileMessage)
                {
                    DrainRawEvents();
                }
            }
        }
        finally
        {
            if (_hook != nint.Zero)
            {
                _api.UnhookWinEvent(_hook);
                _hook = nint.Zero;
            }

            _observations.Writer.TryComplete();
        }
    }

    private void OnWinEvent(
        nint hook,
        uint eventType,
        nint hwnd,
        int idObject,
        int idChild,
        uint eventThreadId,
        uint eventTime)
    {
        if (Volatile.Read(ref _disposed) != 0 || hook != _hook || eventType != EventSystemForeground)
        {
            return;
        }

        var envelope = new RawForegroundEnvelope(
            ++_sequence,
            Stopwatch.GetTimestamp(),
            hwnd);

        _rawEvents.Writer.TryWrite(envelope);
        _api.PostThreadMessage(_workerThreadId, ReconcileMessage, nint.Zero, nint.Zero);
    }

    private void DrainRawEvents()
    {
        while (_rawEvents.Reader.TryRead(out var envelope))
        {
            var identity = ResolveIdentity(envelope.NativeWindowHandle);
            if (identity is not null)
            {
                _observations.Writer.TryWrite(new ForegroundApplicationContextChanged(
                    envelope.ObservationSequence,
                    envelope.ObservedAtMonotonicTimestamp,
                    identity));
            }
        }
    }

    private ForegroundApplicationIdentity? ResolveIdentity(nint hwnd)
    {
        if (hwnd == nint.Zero)
        {
            return null;
        }

        var processId = _api.GetWindowProcessId(hwnd);
        if (processId == 0 || processId == _ownProcessId)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            var application = ForegroundApplicationIdentityNormalizer.Normalize(process.ProcessName);
            return application is null
                ? null
                : new ForegroundApplicationIdentity(application.Value, (int)processId, hwnd);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private readonly record struct RawForegroundEnvelope(
        ulong ObservationSequence,
        long ObservedAtMonotonicTimestamp,
        nint NativeWindowHandle);
}

internal static class ForegroundApplicationIdentityNormalizer
{
    internal static DraftRescue.Domain.Context.ApplicationId? Normalize(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return null;
        }

        var value = processName.Trim();
        if (value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^4];
        }

        if (value.Length is 0 or > 64 || value.Any(char.IsControl))
        {
            return null;
        }

        return new DraftRescue.Domain.Context.ApplicationId(value.ToLowerInvariant());
    }
}

internal interface IWinEventApi
{
    void EnsureMessageQueue();

    uint GetCurrentThreadId();

    nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        uint flags,
        WinEventCallback callback);

    bool UnhookWinEvent(nint hook);

    bool PostThreadMessage(uint threadId, uint message, nint wParam, nint lParam);

    int GetMessage(out NativeMessage message);

    uint GetWindowProcessId(nint hwnd);
}

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
internal delegate void WinEventCallback(
    nint hook,
    uint eventType,
    nint hwnd,
    int idObject,
    int idChild,
    uint eventThreadId,
    uint eventTime);

[StructLayout(LayoutKind.Sequential)]
internal struct NativeMessage
{
    internal nint HWnd;
    internal uint Message;
    internal nint WParam;
    internal nint LParam;
    internal uint Time;
    internal int X;
    internal int Y;
}

internal sealed class NativeWinEventApi : IWinEventApi
{
    internal static NativeWinEventApi Instance { get; } = new();

    public void EnsureMessageQueue() => NativeMethods.PeekMessage(out _, nint.Zero, 0, 0, 0);

    public uint GetCurrentThreadId() => NativeMethods.GetCurrentThreadId();

    public nint SetWinEventHook(uint eventMin, uint eventMax, uint flags, WinEventCallback callback) =>
        NativeMethods.SetWinEventHook(eventMin, eventMax, nint.Zero, callback, 0, 0, flags);

    public bool UnhookWinEvent(nint hook) => NativeMethods.UnhookWinEvent(hook);

    public bool PostThreadMessage(uint threadId, uint message, nint wParam, nint lParam) =>
        NativeMethods.PostThreadMessage(threadId, message, wParam, lParam);

    public int GetMessage(out NativeMessage message) => NativeMethods.GetMessage(out message, nint.Zero, 0, 0);

    public uint GetWindowProcessId(nint hwnd)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        return processId;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PeekMessage(
            out NativeMessage message,
            nint hwnd,
            uint filterMin,
            uint filterMax,
            uint removeMessage);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint SetWinEventHook(
            uint eventMin,
            uint eventMax,
            nint module,
            WinEventCallback callback,
            uint processId,
            uint threadId,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWinEvent(nint hook);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostThreadMessage(
            uint threadId,
            uint message,
            nint wParam,
            nint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetMessage(
            out NativeMessage message,
            nint hwnd,
            uint filterMin,
            uint filterMax);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
    }
}
