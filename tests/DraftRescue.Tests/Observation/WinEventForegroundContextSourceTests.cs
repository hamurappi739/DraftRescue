using DraftRescue.Platform.Windows.Observation;
using Xunit;

namespace DraftRescue.Tests.Observation;

public sealed class WinEventForegroundContextSourceTests
{
    [Theory]
    [InlineData("NOTEPAD", "notepad")]
    [InlineData("notepad.exe", "notepad")]
    [InlineData("  Edge  ", "edge")]
    public void ProcessNameNormalization_IsBoundedAndStable(string processName, string expected)
    {
        var normalized = ForegroundApplicationIdentityNormalizer.Normalize(processName);

        Assert.NotNull(normalized);
        Assert.Equal(expected, normalized.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bad\u0000name")]
    public void InvalidProcessName_IsRejected(string? processName)
    {
        Assert.Null(ForegroundApplicationIdentityNormalizer.Normalize(processName));
    }

    [Fact]
    public async Task Dispose_UnhooksForegroundSourceExactlyOnce()
    {
        var api = new FakeWinEventApi();
        var source = new WinEventForegroundContextSource(api, uint.MaxValue);

        _ = source.WatchAsync(CancellationToken.None);
        await source.DisposeAsync();
        await source.DisposeAsync();

        Assert.Equal(1, api.SetHookCount);
        Assert.Equal(1, api.UnhookCount);
    }

    [Fact]
    public async Task ForegroundCallback_EmitsContentFreeApplicationIdentity()
    {
        var api = new FakeWinEventApi();
        await using var source = new WinEventForegroundContextSource(api, uint.MaxValue);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = source.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        var next = enumerator.MoveNextAsync().AsTask();
        api.RaiseForeground(17);

        Assert.True(await next.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken));
        Assert.Equal((nint)17, enumerator.Current.Identity!.Value.NativeWindowHandle);
        Assert.True(enumerator.Current.ObservationSequence > 0);
        Assert.Equal(0, api.ContentReadCount);

        await enumerator.DisposeAsync();
    }

    private sealed class FakeWinEventApi : IWinEventApi
    {
        private readonly AutoResetEvent _messageReady = new(false);
        private WinEventCallback? _callback;
        private uint _queuedMessage;

        public int SetHookCount { get; private set; }

        public int UnhookCount { get; private set; }

        public int ContentReadCount { get; private set; }

        public void EnsureMessageQueue()
        {
        }

        public uint GetCurrentThreadId() => 123;

        public nint SetWinEventHook(uint eventMin, uint eventMax, uint flags, WinEventCallback callback)
        {
            SetHookCount++;
            _callback = callback;
            return 1;
        }

        public bool UnhookWinEvent(nint hook)
        {
            UnhookCount++;
            return true;
        }

        public bool PostThreadMessage(uint threadId, uint message, nint wParam, nint lParam)
        {
            _queuedMessage = message;
            _messageReady.Set();
            return true;
        }

        public int GetMessage(out NativeMessage message)
        {
            _messageReady.WaitOne(TimeSpan.FromSeconds(2));
            message = new NativeMessage { Message = _queuedMessage };
            return 1;
        }

        public uint GetWindowProcessId(nint hwnd) => (uint)Environment.ProcessId;

        public void RaiseForeground(nint hwnd) => _callback!(1, 0x0003, hwnd, 0, 0, 0, 0);
    }
}
