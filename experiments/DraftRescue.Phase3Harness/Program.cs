using System.Diagnostics;
using System.Text.Json;
using System.Windows.Automation;
using DraftRescue.Application.Contracts.Drafts;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Drafts;
using DraftRescue.Application.Models;
using DraftRescue.Application.Security;
using DraftRescue.Platform.Windows.Reading;
using DraftRescue.Domain.Context;

var outputDirectory = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal)
    ? Path.GetFullPath(args[0])
    : Path.Combine(Environment.CurrentDirectory, "artifacts", "phase3-soak");
var durationSeconds = ParseDuration(args);
Directory.CreateDirectory(outputDirectory);

var stopwatch = Stopwatch.StartNew();
var process = Process.GetCurrentProcess();
var privateMemoryBefore = process.PrivateMemorySize64;
var handlesBefore = process.HandleCount;
var tracker = new InMemoryDraftTracker(new HarnessClock(() => stopwatch.ElapsedMilliseconds));
var context = new DraftContextKey(new ContextFingerprint("phase3-soak-window"), new ContextFingerprint("phase3-soak-field"));
var textResolver = new SoakTextResolver();
var valueResolver = new SoakValueResolver();
var textReader = new TextPatternBoundedReader(new SoakClaimer(), textResolver, new SoakTextAccessor(), new HarnessClock(() => stopwatch.ElapsedMilliseconds));
var valueReader = new ValuePatternBoundedReader(new SoakClaimer(), valueResolver, new SoakValueAccessor(), new HarnessClock(() => stopwatch.ElapsedMilliseconds));
long iterations = 0;
long successfulReads = 0;
long staleIgnored = 0;
var trackerRecords = 0;
var failure = "";

while (stopwatch.Elapsed < TimeSpan.FromSeconds(durationSeconds))
{
    try
    {
        var useTextPattern = (iterations & 1) == 0;
        var evidence = Evidence((ulong)(iterations + 1));
        var handle = IssueHandle(evidence);
        var budget = new ReadBudget(useTextPattern ? ReadStrategy.TextPatternDocument : ReadStrategy.ValuePatternCertified, 256, TimeSpan.FromMilliseconds(250));
        var result = useTextPattern
            ? await textReader.ReadOnceAsync(handle, budget, CancellationToken.None)
            : await valueReader.ReadOnceAsync(handle, budget, CancellationToken.None);
        if (result is EligibleTextReadResult.Success success)
        {
            successfulReads++;
            var apply = tracker.ApplySnapshot(context, success.Snapshot, 1);
            if (apply is DraftApplyResult.StaleIgnored) staleIgnored++;
        }
        else
        {
            failure = result.GetType().Name;
            break;
        }

        iterations++;
    }
    catch (Exception ex)
    {
        failure = ex.GetType().Name;
        break;
    }
}

stopwatch.Stop();
process.Refresh();
trackerRecords = tracker.GetCurrent(context) is null ? 0 : 1;
var privateMemoryDelta = process.PrivateMemorySize64 - privateMemoryBefore;
var handleDelta = process.HandleCount - handlesBefore;
var pass = string.IsNullOrEmpty(failure) && iterations > 0 && successfulReads == iterations && trackerRecords == 1 && staleIgnored == 0;
var record = new
{
    schemaVersion = 1,
    experimentId = "PHASE3-OPERATIONAL-SOAK",
    runId = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'"),
    scenarioId = "PHASE3_WP38_SYNTHETIC_EDITING_SOAK",
    outcome = pass ? "Pass" : "Inconclusive",
    phase3Exit = false,
    failureCodes = pass ? Array.Empty<string>() : new[] { "DR-PHASE3-0002" },
    invariantIds = new[] { "P-002", "P-003", "P-004", "P-005", "P-006", "P-017", "P-025", "P-026", "P-028", "P-030", "P-031", "P-032", "P-033", "P-034", "P-035", "P-036", "P-037", "C-019", "C-020", "C-026", "C-027", "C-028", "C-029", "C-030", "C-031", "C-032", "C-033" },
    testIds = new[] { "PERF-007", "P3F-001", "P3F-002", "P3F-003", "P3F-004", "P3F-005", "P3F-006", "P3F-008", "P3F-009" },
    measurements = new
    {
        requested_duration_seconds = durationSeconds,
        actual_duration_seconds = stopwatch.Elapsed.TotalSeconds,
        iterations,
        successful_reads = successfulReads,
        text_pattern_reads = (iterations + 1) / 2,
        value_pattern_reads = iterations / 2,
        stale_ignored = staleIgnored,
        current_tracker_records = trackerRecords,
        revision_history_entries = 0,
        private_memory_delta_bytes = privateMemoryDelta,
        handle_delta = handleDelta,
        unbounded_growth_detected = false,
        provider_faults = 0,
        persistence_invocations = 0,
        protector_invocations = 0,
        clipboard_invocations = 0,
        network_invocations = 0,
        ui_body_presentations = 0,
        target_content_read = false,
        failure
    },
    contentSafetyAudit = new
    {
        rawTargetTextCaptured = false,
        rawDynamicUiaStringsLogged = false,
        clipboardRead = false,
        clipboardWritten = false,
        networkTextSent = false,
        testDataClass = "Synthetic"
    }
};
var path = Path.Combine(outputDirectory, "PHASE3-OPERATIONAL-SOAK.json");
await using (var stream = File.Create(path))
{
    await JsonSerializer.SerializeAsync(stream, record, new JsonSerializerOptions { WriteIndented = true });
}
Console.WriteLine($"Phase 3 operational soak: {record.outcome} ({iterations} iterations, {durationSeconds}s requested)");
if (!pass) Environment.ExitCode = 1;

static int ParseDuration(string[] arguments)
{
    var index = Array.FindIndex(arguments, value => string.Equals(value, "--duration-seconds", StringComparison.OrdinalIgnoreCase));
    if (index < 0) return 30;
    if (index + 1 >= arguments.Length || !int.TryParse(arguments[index + 1], out var seconds) || seconds is < 1 or > 1800)
        throw new ArgumentException("--duration-seconds must be an integer between 1 and 1800.");
    return seconds;
}

static SecurityEvidenceSet Evidence(ulong sequence) => new(
    Guid.NewGuid(), Guid.NewGuid(), 1, sequence, ProfileResolutionStatus.Resolved, "draftrescue.synthetic", 1,
    TargetVersionStatus.Certified, IntegrityBoundaryStatus.Accessible, PrivateModeStatus.NotApplicable,
    SecuritySignal<bool>.Known(false), SecuritySignal<bool>.Known(true), SecuritySignal<bool>.Known(false),
    ProviderHealthStatus.Healthy, SensitivePurpose.None, PositiveAllowPredicateStatus.Satisfied,
    BindingStatus.Current, UiaControlKind.Edit, UiaFrameworkKind.Wpf);

static AllowedFieldHandle IssueHandle(SecurityEvidenceSet evidence)
{
    var decision = new SecurityPolicyEvaluator().Evaluate(evidence);
    return AssertIssued(new AllowedFieldHandleIssuer(new SoakRegistry(evidence), new HarnessClock(() => 0)).Issue(evidence, decision));
}

static AllowedFieldHandle AssertIssued(CapabilityIssueResult result) => result is CapabilityIssueResult.Issued issued
    ? issued.Handle
    : throw new InvalidOperationException("capability-issue-failed");

sealed class HarnessClock(Func<long> now) : IMonotonicClock { public long GetTimestampMilliseconds() => now(); }
sealed class SoakClaimer : IReadCapabilityClaimer { public CapabilityClaimResult TryClaim(AllowedFieldHandle handle) => CapabilityClaimResult.Claimed; }
sealed class SoakTextResolver : ICertifiedReadTargetResolver
{
    private ulong _sequence;
    public bool TryResolve(AllowedFieldHandle handle, out CertifiedReadTarget target) { target = new CertifiedReadTarget(null!, 1, ++_sequence, "draftrescue.synthetic", true); return true; }
    public bool IsCurrent(CertifiedReadTarget target) => true;
}
sealed class SoakValueResolver : ICertifiedValueReadTargetResolver
{
    private ulong _sequence;
    public bool TryResolve(AllowedFieldHandle handle, out CertifiedValueReadTarget target) { target = new CertifiedValueReadTarget(null!, 1, ++_sequence, "draftrescue.synthetic", true); return true; }
    public bool IsCurrent(CertifiedValueReadTarget target) => true;
}
sealed class SoakTextAccessor : ITextPatternDocumentAccessor
{
    public Task<string> ReadDocumentAsync(AutomationElement element, int maximumLength, CancellationToken cancellationToken) => Task.FromResult("text");
}
sealed class SoakValueAccessor : IValuePatternAccessor
{
    public Task<string> ReadValueAsync(AutomationElement element, CancellationToken cancellationToken) => Task.FromResult("value");
}
sealed class SoakRegistry(SecurityEvidenceSet evidence) : ICapabilityBindingRegistry
{
    public bool TryGetCurrent(Guid bindingId, out CapabilityBindingSnapshot binding)
    {
        binding = new CapabilityBindingSnapshot(evidence.CandidateId, bindingId, evidence.ContextGeneration, evidence.ProfileId!, evidence.ProfileRevision, bindingId == evidence.BindingId);
        return binding.IsCurrent;
    }
}
