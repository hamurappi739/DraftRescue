using DraftRescue.Application.Models;
using DraftRescue.Platform.Windows.Automation;
using Xunit;

namespace DraftRescue.Tests.Observation;

public sealed class UiaFocusedElementMetadataSourceTests
{
    [Fact]
    public async Task FocusChanged_EmitsOnlyBoundedMetadata()
    {
        var api = new FakeUiaFocusApi
        {
            Metadata = new FocusedElementMetadata(
                123,
                (nint)456,
                UiaControlKind.Edit,
                UiaFrameworkKind.Win32,
                UiaBooleanSignal.True,
                UiaBooleanSignal.False,
                UiaBooleanSignal.False,
                UiaBooleanSignal.True,
                UiaBooleanSignal.True,
                UiaBooleanSignal.True,
                UiaBooleanSignal.False,
                "automation-token",
                "Edit",
                UiaCapabilityHints.ValuePatternAvailable)
        };
        await using var source = new UiaFocusedElementMetadataSource(api);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = source.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        var next = enumerator.MoveNextAsync().AsTask();
        api.RaiseFocusChanged();

        Assert.True(await next.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken));
        Assert.Equal(UiaControlKind.Edit, enumerator.Current.Metadata!.ControlKind);
        Assert.Equal("automation-token", enumerator.Current.Metadata.AutomationIdToken);
        Assert.InRange(api.ReadCount, 1, 2);
        var propertyNames = typeof(FocusedElementMetadata).GetProperties().Select(x => x.Name);
        Assert.DoesNotContain("Text", propertyNames);
        Assert.DoesNotContain("Value", propertyNames);
        Assert.DoesNotContain("Content", propertyNames);

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_UnregistersFocusHandlerExactlyOnce()
    {
        var api = new FakeUiaFocusApi();
        var source = new UiaFocusedElementMetadataSource(api);

        _ = source.WatchAsync(TestContext.Current.CancellationToken);
        await source.DisposeAsync();
        await source.DisposeAsync();

        Assert.Equal(1, api.RegisterCount);
        Assert.Equal(1, api.UnregisterCount);
    }

    [Fact]
    public async Task Startup_PerformsOneInitialMetadataReconciliation()
    {
        var api = new FakeUiaFocusApi
        {
            Metadata = new FocusedElementMetadata(
                123,
                (nint)456,
                UiaControlKind.Edit,
                UiaFrameworkKind.Win32,
                UiaBooleanSignal.True,
                UiaBooleanSignal.False,
                UiaBooleanSignal.False,
                UiaBooleanSignal.True,
                UiaBooleanSignal.True,
                UiaBooleanSignal.True,
                UiaBooleanSignal.False,
                "automation-token",
                "Edit",
                UiaCapabilityHints.None)
        };
        await using var source = new UiaFocusedElementMetadataSource(api);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = source.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2), cancellationToken));
        Assert.Equal(UiaControlKind.Edit, enumerator.Current.Metadata!.ControlKind);
        Assert.Equal(1, api.ReadCount);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task HungProvider_DoesNotBlockBoundedDispose()
    {
        var api = new FakeUiaFocusApi { BlockReads = true };
        await using var source = new UiaFocusedElementMetadataSource(api);
        _ = source.WatchAsync(TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);

        Assert.True(api.ReadStarted.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var disposeTask = source.DisposeAsync().AsTask();
        await disposeTask;
        stopwatch.Stop();

        Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(6));
        api.ReleaseRead.Set();
        Assert.True(SpinWait.SpinUntil(() => api.UnregisterCount == 1, TimeSpan.FromSeconds(2)));
        Assert.Equal(1, api.UnregisterCount);
    }

    [Fact]
    public async Task ProviderFailure_IsCountedWithoutExposingExceptionDetails()
    {
        var api = new FakeUiaFocusApi { ThrowOnRead = true };
        await using var source = new UiaFocusedElementMetadataSource(api);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = source.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2), cancellationToken));
        Assert.Null(enumerator.Current.Metadata);
        Assert.Equal(1, source.MetadataFailureCount);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task RepeatedProviderFailures_UseBoundedBackoff()
    {
        var api = new FakeUiaFocusApi { ThrowOnRead = true };
        await using var source = new UiaFocusedElementMetadataSource(api);
        var cancellationToken = TestContext.Current.CancellationToken;
        var enumerator = source.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2), cancellationToken));
        Assert.Null(enumerator.Current.Metadata);

        for (var index = 0; index < 100; index++)
        {
            api.RaiseFocusChanged();
        }

        await Task.Delay(50, cancellationToken);
        // The provider worker may observe one additional signal that arrived
        // during the bounded backoff window, but it must not spin continuously.
        Assert.InRange(api.ReadCount, 1, 2);
        Assert.True(source.BackoffSuppressedCount > 0);

        api.ThrowOnRead = false;
        await Task.Delay(350, cancellationToken);
        api.RaiseFocusChanged();
        Assert.True(SpinWait.SpinUntil(() => api.ReadCount >= 2, TimeSpan.FromSeconds(2)));
        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2), cancellationToken));
        Assert.InRange(source.MetadataFailureCount, 1, 2);
        await enumerator.DisposeAsync();
    }

    private sealed class FakeUiaFocusApi : IUiaFocusApi
    {
        private Action? _callback;

        public FocusedElementMetadata? Metadata { get; init; }

        public int RegisterCount { get; private set; }

        public int UnregisterCount { get; private set; }

        public int ReadCount { get; private set; }

        public bool BlockReads { get; init; }

        public bool ThrowOnRead { get; set; }

        public ManualResetEventSlim ReadStarted { get; } = new(false);

        public ManualResetEventSlim ReleaseRead { get; } = new(false);

        public void RegisterFocusChanged(Action callback)
        {
            RegisterCount++;
            _callback = callback;
        }

        public void UnregisterFocusChanged(Action callback)
        {
            UnregisterCount++;
            _callback = null;
        }

        public FocusedElementMetadata? ReadFocusedElementMetadata()
        {
            ReadCount++;
            if (ThrowOnRead)
            {
                throw new InvalidOperationException("synthetic provider failure");
            }

            if (BlockReads)
            {
                ReadStarted.Set();
                ReleaseRead.Wait(TimeSpan.FromSeconds(30));
            }

            return Metadata;
        }

        public void RaiseFocusChanged() => _callback!();
    }
}
