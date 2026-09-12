using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Contracts.Retention;
using DraftRescue.Desktop.Persistence;
using DraftRescue.Infrastructure.Persistence;
using DraftRescue.Platform.Windows.Storage;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4DesktopPersistenceTests
{
    [Fact]
    public async Task StartupCreatesProtectedStoreAndRunsCleanupBeforeRuntime()
    {
        var root = Path.Combine(Path.GetTempPath(), "DraftRescue-WP48-runtime-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = await DesktopPersistenceRuntime.StartAsync(new WindowsLocalDataPathProvider(root), new FakeClock(), TestContext.Current.CancellationToken);

            Assert.Equal(DesktopPersistenceAvailability.Ready, result.Availability);
            Assert.NotNull(result.Runtime);
            Assert.True(File.Exists(Path.Combine(root, "db", "drafts.db")));
            Assert.Equal(1, result.Runtime!.RetentionHealth.AttemptCount);

            await result.Runtime.DisposeAsync();
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task CorruptStoreIsReportedWithoutReplacement()
    {
        var root = Path.Combine(Path.GetTempPath(), "DraftRescue-WP48-corrupt-runtime-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(root, "db", "drafts.db");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(database)!);
            File.WriteAllText(database, "not a sqlite database");

            var result = await DesktopPersistenceRuntime.StartAsync(new WindowsLocalDataPathProvider(root), new FakeClock(), TestContext.Current.CancellationToken);

            Assert.Equal(DesktopPersistenceAvailability.Corrupt, result.Availability);
            Assert.Null(result.Runtime);
            Assert.Equal("not a sqlite database", File.ReadAllText(database));
        }
        finally { Delete(root); }
    }

    private sealed class FakeClock : IWallClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch.AddMinutes(30);
    }

    private static void Delete(string path)
    {
        for (var attempt = 0; attempt < 5 && Directory.Exists(path); attempt++)
        {
            try { Directory.Delete(path, recursive: true); }
            catch (IOException) { GC.Collect(); GC.WaitForPendingFinalizers(); Thread.Sleep(50); }
        }
    }
}
