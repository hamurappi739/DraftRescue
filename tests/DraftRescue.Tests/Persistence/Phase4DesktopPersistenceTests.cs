using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Contracts.Retention;
using DraftRescue.Application.Models;
using DraftRescue.Application.Persistence;
using DraftRescue.Application.Security;
using DraftRescue.Domain.Drafts;
using DraftRescue.Desktop.Persistence;
using DraftRescue.Infrastructure.Persistence;
using DraftRescue.Platform.Windows.Security;
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
            var result = await DesktopPersistenceRuntime.StartAsync(new WindowsLocalDataPathProvider(root), new FakeClock(), new FakeSecretProvider(), new WindowsDpapiDraftProtector(), TestContext.Current.CancellationToken);

            Assert.Equal(DesktopPersistenceAvailability.Ready, result.Availability);
            Assert.NotNull(result.Runtime);
            Assert.NotNull(result.Runtime!.CheckpointCoordinator);
            Assert.True(File.Exists(Path.Combine(root, "db", "drafts.db")));
            Assert.Equal(1, result.Runtime.RetentionHealth.AttemptCount);

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

            var result = await DesktopPersistenceRuntime.StartAsync(new WindowsLocalDataPathProvider(root), new FakeClock(), new FakeSecretProvider(), new WindowsDpapiDraftProtector(), TestContext.Current.CancellationToken);

            Assert.Equal(DesktopPersistenceAvailability.Corrupt, result.Availability);
            Assert.Null(result.Runtime);
            Assert.Equal("not a sqlite database", File.ReadAllText(database));
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task ProtectionStoreFailureStopsStartupBeforeDatabaseCreation()
    {
        var root = Path.Combine(Path.GetTempPath(), "DraftRescue-WP48-secret-failure-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = await DesktopPersistenceRuntime.StartAsync(
                new WindowsLocalDataPathProvider(root),
                new FakeClock(),
                new FailingSecretProvider(),
                new WindowsDpapiDraftProtector(),
                TestContext.Current.CancellationToken);

            Assert.Equal(DesktopPersistenceAvailability.Unavailable, result.Availability);
            Assert.Null(result.Runtime);
            Assert.False(File.Exists(Path.Combine(root, "db", "drafts.db")));
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task ExplicitPolicyComposesBackgroundCheckpointWorkerIntoRuntime()
    {
        var root = Path.Combine(Path.GetTempPath(), "DraftRescue-WP45-runtime-worker-" + Guid.NewGuid().ToString("N"));
        DesktopPersistenceRuntime? runtime = null;
        try
        {
            var result = await DesktopPersistenceRuntime.StartAsync(
                new WindowsLocalDataPathProvider(root),
                new FakeClock(),
                new FakeSecretProvider(),
                new FakeProtector(),
                TestContext.Current.CancellationToken);
            runtime = result.Runtime;
            Assert.Equal(DesktopPersistenceAvailability.Ready, result.Availability);
            Assert.NotNull(runtime);

            var monotonic = new FakeMonotonicClock();
            var policy = CheckpointSchedulePolicy.Create(
                TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(250), 2);
            runtime!.StartCheckpointWorker(policy, monotonic, TestContext.Current.CancellationToken);
            Assert.True(runtime.IsCheckpointWorkerRunning);
            Assert.Throws<InvalidOperationException>(() => runtime.StartCheckpointWorker(policy, monotonic, TestContext.Current.CancellationToken));

            var candidate = Candidate(DraftId.New());
            Assert.Equal(CheckpointScheduleOutcome.Scheduled, runtime.ScheduleCheckpoint(candidate).Outcome);
            monotonic.Now = 100;
            Assert.Equal(CheckpointScheduleOutcome.StaleIgnored, runtime.ScheduleCheckpoint(candidate).Outcome);

            ProtectedDraftRecordV1? persisted = null;
            for (var attempt = 0; attempt < 50 && persisted is null; attempt++)
            {
                await Task.Delay(10, TestContext.Current.CancellationToken);
                persisted = await runtime.Repository.GetProtectedAsync(candidate.DraftId, TestContext.Current.CancellationToken);
            }

            Assert.NotNull(persisted);
            Assert.Equal(1L, persisted!.SnapshotSequence);
            Assert.Equal(new byte[] { 0xD, 0xA, 0x4 }, persisted.ProtectedPayloadBytes.ToArray());

            var runningRuntime = runtime;
            await runningRuntime.DisposeAsync();
            runtime = null;
            Assert.False(runningRuntime.IsCheckpointWorkerRunning);
            Assert.Throws<ObjectDisposedException>(() => runningRuntime.ScheduleCheckpoint(candidate));
        }
        finally
        {
            if (runtime is not null) await runtime.DisposeAsync();
            Delete(root);
        }
    }

    private static CheckpointCandidate Candidate(DraftId draftId)
    {
        var snapshot = FieldTextSnapshot.Create(
            contextGeneration: 1,
            snapshotSequence: 1,
            captureAttemptId: Guid.NewGuid(),
            profileId: "test.profile",
            readStrategy: ReadStrategy.ValuePatternCertified,
            capturedAtMonotonic: 1,
            capturedAtUtc: DateTimeOffset.UnixEpoch,
            text: "runtime worker payload");
        return CheckpointCandidate.Create(
            draftId, 1, 1, snapshot, "test.app", null, null,
            DraftPresentationKind.GenericText, 1, 1, new byte[] { 1 }, TimeSpan.FromMinutes(30));
    }

    private sealed class FakeClock : IWallClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch.AddMinutes(30);
    }

    private sealed class FakeSecretProvider : IInstallationSecretProvider
    {
        public Task<InstallationSecret> GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(InstallationSecret.Create(new byte[32]));
        }
    }

    private sealed class FailingSecretProvider : IInstallationSecretProvider
    {
        public Task<InstallationSecret> GetOrCreateAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<InstallationSecret>(new DraftProtectionException(DraftProtectionFailureCode.DpapiFailure));
    }

    private sealed class FakeMonotonicClock : IMonotonicClock
    {
        public long Now;
        public long GetTimestampMilliseconds() => Volatile.Read(ref Now);
    }

    private sealed class FakeProtector : IDraftProtector
    {
        public ProtectedDraftPayload Protect(DraftPlaintextPayload plaintext, DraftProtectionContext context) =>
            ProtectedDraftPayload.Create(1, new byte[] { 0xD, 0xA, 0x4 });

        public DraftPlaintextPayload Unprotect(ProtectedDraftPayload protectedPayload, DraftProtectionContext context) =>
            throw new NotSupportedException();
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
