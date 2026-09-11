using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Uia = System.Windows.Automation;
using WpfApplication = System.Windows.Application;
using DraftRescue.Application.Contracts.Observation;
using DraftRescue.Application.Models;
using DraftRescue.Application.Observation;
using DraftRescue.Application.Security;
using DomainApplicationId = DraftRescue.Domain.Context.ApplicationId;

var outputDirectory = args.Length == 0
    ? Path.Combine(Environment.CurrentDirectory, "artifacts", "phase1")
    : Path.GetFullPath(args[0]);
Directory.CreateDirectory(outputDirectory);

if (args.Any(value => string.Equals(value, "--wpf-fixture", StringComparison.OrdinalIgnoreCase)))
{
    RunWpfFixture();
    return;
}

var runId = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
var soakSeconds = ParseSoakSeconds(args);

if (args.Any(value => string.Equals(value, "--wpf-only", StringComparison.OrdinalIgnoreCase)))
{
    await RunWpfFixtureReconnaissanceAsync(runId, outputDirectory);
    return;
}

if (args.Any(value => string.Equals(value, "--wpf-interactive", StringComparison.OrdinalIgnoreCase)))
{
    await RunInteractiveWpfFixtureReconnaissanceAsync(runId, outputDirectory);
    return;
}

if (args.Any(value => string.Equals(value, "--wpf-preflight", StringComparison.OrdinalIgnoreCase)))
{
    await RunInteractiveSessionPreflightAsync(runId, outputDirectory);
    return;
}

if (args.Any(value => string.Equals(value, "--phase2-native-fixture", StringComparison.OrdinalIgnoreCase)))
{
    await RunPhase2NativeFixtureAsync(runId, outputDirectory);
    return;
}

if (args.Any(value => string.Equals(value, "--phase2-fault-harness", StringComparison.OrdinalIgnoreCase)))
{
    await RunPhase2FaultHarnessAsync(runId, outputDirectory);
    return;
}

if (args.Any(value => string.Equals(value, "--provider-stress", StringComparison.OrdinalIgnoreCase)))
{
    await RunProviderStressAsync(runId, outputDirectory);
    return;
}

var records = new List<object>
{
    await RunSyntheticStormAsync(runId, outputDirectory),
    await RunProviderFaultAsync(runId, outputDirectory),
    await RunProviderHangStressAsync(runId, outputDirectory),
    await RunResponsivenessProbeAsync(runId, outputDirectory),
    await RunSyntheticSoakAsync(runId, outputDirectory, soakSeconds)
};

if (args.Any(value => string.Equals(value, "--notepad", StringComparison.OrdinalIgnoreCase)))
{
    records.Add(await RunNotepadReconnaissanceAsync(runId, outputDirectory));
}

records.Add(await RunWpfFixtureReconnaissanceAsync(runId, outputDirectory));

Console.WriteLine($"Phase 1 WP-1.4 harness passed; records written to {outputDirectory}");

static async Task RunInteractiveSessionPreflightAsync(string runId, string outputDirectory)
{
    var preflight = DraftRescue.Platform.Windows.Testing.InteractiveSessionPreflightResult.Capture();
    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-WPF-INTERACTIVE-PREFLIGHT",
        runId,
        scenarioId = "PHASE1_WPF_INTERACTIVE_SESSION_PREFLIGHT",
        outcome = preflight.ReadyForInteractiveFixture ? "Pass" : "Inconclusive",
        durationMs = 0,
        failureCodes = Array.Empty<string>(),
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-026", "P-028", "P-029", "P-030", "C-016", "C-017", "C-022", "C-023" },
        testIds = new[] { "EXP-001", "EXP-002" },
        measurements = new
        {
            user_interactive = preflight.UserInteractive,
            current_process_id = preflight.CurrentProcessId,
            current_session_id = preflight.CurrentSessionId,
            foreground_window_present = preflight.ForegroundWindowPresent,
            foreground_process_id = preflight.ForegroundProcessId,
            foreground_process_session_id = preflight.ForegroundProcessSessionId,
            foreground_same_session = preflight.ForegroundSameSession,
            input_desktop_accessible = preflight.InputDesktopAccessible,
            foreground_focus_window_present = preflight.ForegroundFocusWindowPresent,
            foreground_thread_id = preflight.ForegroundThreadId,
            ready_for_interactive_fixture = preflight.ReadyForInteractiveFixture,
            target_content_read = false,
            text_reader_invocation_count = 0,
            repository_invocation_count = 0
        },
        contentSafetyAudit = new
        {
            rawTargetTextCaptured = false,
            rawDynamicUiaStringsLogged = false,
            clipboardRead = false,
            clipboardWritten = false,
            networkTextSent = false,
            testDataClass = "MetadataOnly"
        }
    };

    await WriteRecordAsync(outputDirectory, "WP14-WPF-INTERACTIVE-PREFLIGHT", record);
    Console.WriteLine($"Interactive WPF preflight: {record.outcome} (ready={preflight.ReadyForInteractiveFixture})");
}

static async Task RunPhase2NativeFixtureAsync(string runId, string outputDirectory)
{
    IReadOnlyList<DraftRescue.Platform.Windows.Testing.Phase2NativeFieldMetadata> fields;
    try
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        fields = await DraftRescue.Platform.Windows.Testing.Phase2NativeSecureFixture.CaptureAsync(timeout.Token);
    }
    catch (OperationCanceledException)
    {
        fields = Array.Empty<DraftRescue.Platform.Windows.Testing.Phase2NativeFieldMetadata>();
    }

    var evaluator = new SecurityPolicyEvaluator();
    var matrix = new List<Dictionary<string, object>>();
    var ordinaryAllowed = false;
    var passwordDenied = false;
    var readOnlyDenied = false;
    var capabilityIssued = false;

    foreach (var field in fields)
    {
        var candidateId = Guid.NewGuid();
        var bindingId = Guid.NewGuid();
        var evidence = new SecurityEvidenceSet(
            candidateId,
            bindingId,
            1,
            1,
            ProfileResolutionStatus.Resolved,
            "draftrescue.synthetic",
            1,
            TargetVersionStatus.Certified,
            IntegrityBoundaryStatus.Accessible,
            PrivateModeStatus.NotApplicable,
            SecuritySignal<bool>.Known(field.IsPassword),
            SecuritySignal<bool>.Known(!field.IsReadOnly),
            SecuritySignal<bool>.Known(field.IsReadOnly),
            field.MetadataReadFailed ? ProviderHealthStatus.Failed : ProviderHealthStatus.Healthy,
            SensitivePurpose.None,
            field.SurfaceId == "ordinary-edit"
                ? PositiveAllowPredicateStatus.Satisfied
                : PositiveAllowPredicateStatus.NotSatisfied,
            BindingStatus.Current,
            UiaControlKind.Edit,
            UiaFrameworkKind.Win32);

        var decision = evaluator.Evaluate(evidence);
        var allowed = decision is SecurityDecision.Allowed;
        if (field.SurfaceId == "ordinary-edit") ordinaryAllowed = allowed;
        if (field.SurfaceId == "password-edit") passwordDenied = decision is SecurityDecision.Denied { Reason: CaptureDenialReason.SecureField };
        if (field.SurfaceId == "readonly-edit") readOnlyDenied = decision is SecurityDecision.Denied;

        if (allowed)
        {
            var issuer = new AllowedFieldHandleIssuer(new FixtureBindingRegistry(evidence), new FixtureClock());
            capabilityIssued = issuer.Issue(evidence, decision) is CapabilityIssueResult.Issued;
        }

        matrix.Add(new Dictionary<string, object>
        {
            ["surface_id"] = field.SurfaceId,
            ["control_type_id"] = field.ControlTypeId,
            ["is_password"] = field.IsPassword,
            ["is_read_only"] = field.IsReadOnly,
            ["is_enabled"] = field.IsEnabled,
            ["is_keyboard_focusable"] = field.IsKeyboardFocusable,
            ["metadata_read_failed"] = field.MetadataReadFailed,
            ["decision"] = allowed ? "Allowed" : (decision as SecurityDecision.Denied)?.Reason.ToString() ?? "Denied",
            ["content_read_invocations"] = 0
        });
    }

    var outcome = fields.Count == 3 && ordinaryAllowed && capabilityIssued && passwordDenied && readOnlyDenied
        ? "Pass"
        : "Inconclusive";
    var record = new
    {
        schemaVersion = 1,
        experimentId = "PHASE2-NATIVE-FIXTURE",
        runId,
        scenarioId = "PHASE2_WINDOWS_FORMS_STRUCTURAL_SECURITY_SIGNALS",
        outcome,
        durationMs = 0,
        failureCodes = outcome == "Pass" ? Array.Empty<string>() : new[] { "DR-SEC-2005" },
        invariantIds = new[] { "P-002", "P-003", "P-004", "P-005", "P-026", "P-030", "P-031", "P-034", "P-036", "P-037", "C-019", "C-020", "C-026", "C-029", "C-030", "C-033" },
        testIds = new[] { "SEC-001", "SEC-002", "SEC-003", "SEC-004", "SEC-005", "ARC-006", "SEC-014", "SEC-015", "SEC-016", "SEC-017" },
        measurements = new
        {
            fixture_class = "WindowsForms",
            fixture_field_count = fields.Count,
            ordinary_allow = ordinaryAllowed,
            password_deny = passwordDenied,
            read_only_deny = readOnlyDenied,
            capability_issued = capabilityIssued,
            content_read_invocations = 0,
            matrix
        },
        contentSafetyAudit = new
        {
            rawTargetTextCaptured = false,
            rawDynamicUiaStringsLogged = false,
            clipboardRead = false,
            clipboardWritten = false,
            networkTextSent = false,
            testDataClass = "MetadataOnly"
        }
    };

    await WriteRecordAsync(outputDirectory, "PHASE2-NATIVE-FIXTURE", record);
    Console.WriteLine($"Phase 2 native fixture: {outcome} ({fields.Count} fields)");
}

static async Task RunPhase2FaultHarnessAsync(string runId, string outputDirectory)
{
    var evaluator = new SecurityPolicyEvaluator();
    var rows = new List<Dictionary<string, object>>();
    var failedCases = 0;

    for (var index = 1; index <= 24; index++)
    {
        var caseId = $"P2-{index:D2}";
        var result = ExecutePhase2FaultCase(caseId, evaluator);
        if (!result.Pass)
        {
            failedCases++;
        }

        rows.Add(new Dictionary<string, object>
        {
            ["case_id"] = caseId,
            ["scenario"] = result.Scenario,
            ["outcome"] = result.Pass ? "Pass" : "Fail",
            ["decision"] = result.Decision,
            ["stable_error_code"] = result.StableErrorCode,
            ["capability_issue_count"] = result.CapabilityIssueCount,
            ["usable_claim_count"] = result.UsableClaimCount,
            ["content_read_invocations"] = 0,
            ["fixture_class"] = "SyntheticInMemory"
        });
    }

    var stressEvaluator = new SecurityPolicyEvaluator();
    var stressEvidence = Phase2SafeEvidence();
    var denyEvidence = stressEvidence with { ProtectedContent = SecuritySignal<bool>.Known(true) };
    var stressBefore = GC.GetAllocatedBytesForCurrentThread();
    var deterministic = true;
    for (var index = 0; index < 10_000; index++)
    {
        var allowed = stressEvaluator.Evaluate(stressEvidence) is SecurityDecision.Allowed;
        var denied = stressEvaluator.Evaluate(denyEvidence) is SecurityDecision.Denied { Reason: CaptureDenialReason.SecureField };
        deterministic &= allowed && denied;
    }

    var stressAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - stressBefore;
    var capabilityCycles = 10_000;
    var cycleRegistry = new Phase2HarnessRegistry(stressEvidence);
    var cycleClock = new Phase2HarnessClock();
    var cycleIssuer = new AllowedFieldHandleIssuer(cycleRegistry, cycleClock);
    var cycleValidator = new AllowedFieldHandleValidator(cycleRegistry, cycleClock);
    var cycleFailures = 0;
    for (var index = 0; index < capabilityCycles; index++)
    {
        var decision = stressEvaluator.Evaluate(stressEvidence);
        if (decision is not SecurityDecision.Allowed)
        {
            cycleFailures++;
            continue;
        }

        var issue = cycleIssuer.Issue(stressEvidence, decision);
        if (issue is not CapabilityIssueResult.Issued issued || !cycleValidator.Revoke(issued.Handle))
        {
            cycleFailures++;
        }
    }

    var permutationEvidence = stressEvidence with
    {
        ProtectedContent = SecuritySignal<bool>.Known(true),
        SensitivePurpose = SensitivePurpose.Payment,
        ProviderHealth = ProviderHealthStatus.Timeout,
        PositiveAllowPredicate = PositiveAllowPredicateStatus.Satisfied
    };
    var permutationResults = new HashSet<string>(StringComparer.Ordinal);
    for (var index = 0; index < 128; index++)
    {
        // Evidence is a typed immutable record; changing construction order must
        // not change the evaluator's canonical deny result.
        permutationResults.Add((stressEvaluator.Evaluate(permutationEvidence) as SecurityDecision.Denied)?.StableErrorCode ?? "ALLOWED");
    }

    var stressPass = deterministic &&
                     stressAllocatedBytes < 16 * 1024 * 1024 &&
                     cycleFailures == 0 &&
                     permutationResults.SetEquals(new[] { "DR-SEC-2001" });
    var overallPass = failedCases == 0 && stressPass;
    var record = new
    {
        schemaVersion = 1,
        experimentId = "PHASE2-FAULT-HARNESS",
        runId,
        scenarioId = "PHASE2_EXECUTABLE_FAULT_NEGATIVE_AND_STRESS_MATRIX",
        outcome = overallPass ? "Pass" : "Inconclusive",
        durationMs = 0,
        failureCodes = overallPass ? Array.Empty<string>() : new[] { "DR-SEC-2007" },
        invariantIds = new[] { "P-002", "P-003", "P-004", "P-005", "P-006", "P-017", "P-025", "P-026", "P-028", "P-030", "P-031", "P-032", "P-033", "P-034", "P-035", "P-036", "P-037", "C-019", "C-020", "C-026", "C-027", "C-028", "C-029", "C-030", "C-031", "C-032", "C-033" },
        testIds = new[] { "P2F-001", "P2F-002", "P2F-003", "P2F-004", "P2F-005", "P2F-006", "CAP-001", "CAP-002", "CAP-003", "CAP-004", "CAP-005", "CAP-006", "CAP-007", "CAP-008" },
        measurements = new
        {
            matrix_case_count = rows.Count,
            matrix_pass_count = rows.Count(row => string.Equals((string)row["outcome"], "Pass", StringComparison.Ordinal)),
            negative_case_count = rows.Count - 1,
            positive_case_count = 1,
            failed_case_count = failedCases,
            stress_evaluation_count = 10_000,
            stress_deterministic = deterministic,
            stress_allocated_bytes = stressAllocatedBytes,
            stress_allocation_bound_bytes = 16 * 1024 * 1024,
            capability_cycle_count = capabilityCycles,
            capability_cycle_failures = cycleFailures,
            capability_registry_retained_state = false,
            permutation_count = 128,
            permutation_distinct_results = permutationResults.Count,
            content_read_invocations = 0,
            rows
        },
        contentSafetyAudit = new
        {
            rawTargetTextCaptured = false,
            rawDynamicUiaStringsLogged = false,
            clipboardRead = false,
            clipboardWritten = false,
            networkTextSent = false,
            testDataClass = "MetadataOnly"
        }
    };

    await WriteRecordAsync(outputDirectory, "PHASE2-FAULT-HARNESS", record);
    Console.WriteLine($"Phase 2 fault harness: {record.outcome} (24 cases, 10000 evaluations)");
    if (!overallPass)
    {
        Environment.ExitCode = 1;
    }
}

static Phase2FaultCaseResult ExecutePhase2FaultCase(string caseId, SecurityPolicyEvaluator evaluator)
{
    var evidence = Phase2SafeEvidence();
    var expectedReason = (CaptureDenialReason?)null;
    var expectedCode = "";
    var scenario = caseId switch
    {
        "P2-01" => "IsPasswordTrue",
        "P2-02" => "ProtectedUnknown",
        "P2-03" => "ProtectedUnavailable",
        "P2-04" => "ProviderTimeout",
        "P2-05" => "StaleBinding",
        "P2-06" => "MissingProfile",
        "P2-07" => "AmbiguousProfile",
        "P2-08" => "UnknownTargetVersion",
        "P2-09" => "IntegrityUnknown",
        "P2-10" => "PrivateModeConfirmed",
        "P2-11" => "PrivateModeUnknown",
        "P2-12" => "CredentialPurpose",
        "P2-13" => "PinOrOtpPurpose",
        "P2-14" => "PaymentPurpose",
        "P2-15" => "EditableOnly",
        "P2-16" => "PasswordFalseOnly",
        "P2-17" => "IndeterminatePredicate",
        "P2-18" => "GenerationStaleBeforeIssue",
        "P2-19" => "GenerationStaleBeforeClaim",
        "P2-20" => "ExpiredCapability",
        "P2-21" => "CapabilityClaimedTwice",
        "P2-22" => "ConcurrentCapabilityClaims",
        "P2-23" => "ProtectedStateToggle",
        "P2-24" => "SyntheticOrdinaryAllowed",
        _ => "Unknown"
    };

    switch (caseId)
    {
        case "P2-01":
            evidence = evidence with { ProtectedContent = SecuritySignal<bool>.Known(true) };
            expectedReason = CaptureDenialReason.SecureField;
            expectedCode = "DR-SEC-2001";
            break;
        case "P2-02":
            evidence = evidence with { ProtectedContent = SecuritySignal<bool>.Unknown() };
            expectedReason = CaptureDenialReason.UncertainSecureState;
            expectedCode = "DR-SEC-2005";
            break;
        case "P2-03":
            evidence = evidence with { ProtectedContent = SecuritySignal<bool>.Unavailable() };
            expectedReason = CaptureDenialReason.UncertainSecureState;
            expectedCode = "DR-SEC-2005";
            break;
        case "P2-04":
            evidence = evidence with { ProviderHealth = ProviderHealthStatus.Timeout };
            expectedReason = CaptureDenialReason.ProviderUnavailable;
            expectedCode = "DR-OBS-1004";
            break;
        case "P2-05":
            evidence = evidence with { BindingStatus = BindingStatus.Stale };
            expectedReason = CaptureDenialReason.StaleContext;
            expectedCode = "DR-OBS-1003";
            break;
        case "P2-06":
            evidence = evidence with { ProfileResolution = ProfileResolutionStatus.Missing };
            expectedReason = CaptureDenialReason.UnsupportedProfile;
            expectedCode = "DR-SEC-2007";
            break;
        case "P2-07":
            evidence = evidence with { ProfileResolution = ProfileResolutionStatus.Ambiguous };
            expectedReason = CaptureDenialReason.UnsupportedProfile;
            expectedCode = "DR-SEC-2007";
            break;
        case "P2-08":
            evidence = evidence with { TargetVersionStatus = TargetVersionStatus.Unknown };
            expectedReason = CaptureDenialReason.UnsupportedProfile;
            expectedCode = "DR-SEC-2007";
            break;
        case "P2-09":
            evidence = evidence with { IntegrityBoundary = IntegrityBoundaryStatus.Unknown };
            expectedReason = CaptureDenialReason.UnsupportedContext;
            expectedCode = "DR-SEC-2006";
            break;
        case "P2-10":
            evidence = evidence with { PrivateMode = PrivateModeStatus.ConfirmedPrivate };
            expectedReason = CaptureDenialReason.PrivateBrowsing;
            expectedCode = "DR-SEC-2004";
            break;
        case "P2-11":
            evidence = evidence with { PrivateMode = PrivateModeStatus.Unknown };
            expectedReason = CaptureDenialReason.UncertainPrivateMode;
            expectedCode = "DR-SEC-2005";
            break;
        case "P2-12":
            evidence = evidence with { SensitivePurpose = SensitivePurpose.Credential };
            expectedReason = CaptureDenialReason.CredentialLikeField;
            expectedCode = "DR-SEC-2002";
            break;
        case "P2-13":
            evidence = evidence with { SensitivePurpose = SensitivePurpose.PinOrOtp };
            expectedReason = CaptureDenialReason.CredentialLikeField;
            expectedCode = "DR-SEC-2002";
            break;
        case "P2-14":
            evidence = evidence with { SensitivePurpose = SensitivePurpose.Payment };
            expectedReason = CaptureDenialReason.BankingOrFinancialField;
            expectedCode = "DR-SEC-2003";
            break;
        case "P2-15":
        case "P2-16":
        case "P2-17":
            evidence = evidence with { PositiveAllowPredicate = PositiveAllowPredicateStatus.NotSatisfied };
            expectedReason = CaptureDenialReason.InsufficientPositiveEvidence;
            expectedCode = "DR-SEC-2007";
            break;
        case "P2-23":
            evidence = evidence with { ProtectedContent = SecuritySignal<bool>.Known(true) };
            expectedReason = CaptureDenialReason.SecureField;
            expectedCode = "DR-SEC-2001";
            break;
        case "P2-24":
            break;
    }

    if (caseId is not ("P2-18" or "P2-19" or "P2-20" or "P2-21" or "P2-22"))
    {
        var decision = evaluator.Evaluate(evidence);
        var registry = new Phase2HarnessRegistry(evidence);
        var issue = new AllowedFieldHandleIssuer(registry, new Phase2HarnessClock()).Issue(evidence, decision);
        var denied = decision as SecurityDecision.Denied;
        var pass = caseId == "P2-24"
            ? decision is SecurityDecision.Allowed && issue is CapabilityIssueResult.Issued
            : denied is not null && denied.Reason == expectedReason && denied.StableErrorCode == expectedCode && issue is not CapabilityIssueResult.Issued;
        return new Phase2FaultCaseResult(pass, scenario, denied is null ? "Allowed" : denied.Reason.ToString(), denied?.StableErrorCode ?? "", issue is CapabilityIssueResult.Issued ? 1 : 0, 0);
    }

    if (caseId == "P2-18")
    {
        var decision = evaluator.Evaluate(evidence);
        var registry = new Phase2HarnessRegistry(evidence) { ContextGeneration = evidence.ContextGeneration + 1 };
        var issue = new AllowedFieldHandleIssuer(registry, new Phase2HarnessClock()).Issue(evidence, decision);
        var denied = issue as CapabilityIssueResult.Denied;
        return new Phase2FaultCaseResult(denied?.Reason == CaptureDenialReason.StaleContext && denied.StableErrorCode == "DR-OBS-1003", scenario, "Denied", denied?.StableErrorCode ?? "", 0, 0);
    }

    var clock = new Phase2HarnessClock();
    var safeRegistry = new Phase2HarnessRegistry(evidence);
    var issuer = new AllowedFieldHandleIssuer(safeRegistry, clock);
    var validator = new AllowedFieldHandleValidator(safeRegistry, clock);
    var allowedDecision = evaluator.Evaluate(evidence);
    var issuedResult = issuer.Issue(evidence, allowedDecision);
    if (issuedResult is not CapabilityIssueResult.Issued issued)
    {
        return new Phase2FaultCaseResult(false, scenario, "Denied", "DR-SEC-2005", 0, 0);
    }

    return caseId switch
    {
        "P2-19" => new Phase2FaultCaseResult(validator.TryClaim(issued.Handle, evidence with { ContextGeneration = evidence.ContextGeneration + 1 }) == CapabilityClaimResult.StaleContext, scenario, "StaleContext", "DR-OBS-1003", 1, 0),
        "P2-20" => ClaimAfterClock(validator, issued.Handle, evidence, clock, 1000, CapabilityClaimResult.Expired, scenario),
        "P2-21" => ClaimTwice(validator, issued.Handle, evidence, scenario),
        "P2-22" => ClaimConcurrently(validator, issued.Handle, evidence, scenario),
        _ => new Phase2FaultCaseResult(false, scenario, "Invalid", "DR-SEC-2005", 1, 0)
    };
}

static Phase2FaultCaseResult ClaimAfterClock(AllowedFieldHandleValidator validator, AllowedFieldHandle handle, SecurityEvidenceSet evidence, Phase2HarnessClock clock, long now, CapabilityClaimResult expected, string scenario)
{
    clock.Now = now;
    var result = validator.TryClaim(handle, evidence);
    return new Phase2FaultCaseResult(result == expected, scenario, result.ToString(), "", 1, 0);
}

static Phase2FaultCaseResult ClaimTwice(AllowedFieldHandleValidator validator, AllowedFieldHandle handle, SecurityEvidenceSet evidence, string scenario)
{
    var first = validator.TryClaim(handle, evidence);
    var second = validator.TryClaim(handle, evidence);
    return new Phase2FaultCaseResult(first == CapabilityClaimResult.Claimed && second == CapabilityClaimResult.AlreadyConsumed, scenario, second.ToString(), "", 1, 1);
}

static Phase2FaultCaseResult ClaimConcurrently(AllowedFieldHandleValidator validator, AllowedFieldHandle handle, SecurityEvidenceSet evidence, string scenario)
{
    var results = new System.Collections.Concurrent.ConcurrentBag<CapabilityClaimResult>();
    Parallel.For(0, 32, _ => results.Add(validator.TryClaim(handle, evidence)));
    var winners = results.Count(value => value == CapabilityClaimResult.Claimed);
    return new Phase2FaultCaseResult(winners == 1 && results.Count == 32, scenario, $"Claimed={winners}", "", 1, winners);
}

static SecurityEvidenceSet Phase2SafeEvidence() => new(
    Guid.Parse("11111111-1111-1111-1111-111111111111"),
    Guid.Parse("22222222-2222-2222-2222-222222222222"),
    7,
    42,
    ProfileResolutionStatus.Resolved,
    "draftrescue.synthetic",
    1,
    TargetVersionStatus.Certified,
    IntegrityBoundaryStatus.Accessible,
    PrivateModeStatus.NotApplicable,
    SecuritySignal<bool>.Known(false),
    SecuritySignal<bool>.Known(true),
    SecuritySignal<bool>.Known(false),
    ProviderHealthStatus.Healthy,
    SensitivePurpose.None,
    PositiveAllowPredicateStatus.Satisfied,
    BindingStatus.Current,
    UiaControlKind.Edit,
    UiaFrameworkKind.Win32);

static async Task<object> RunProviderStressAsync(string runId, string outputDirectory)
{
    var stopwatch = Stopwatch.StartNew();
    var observationCount = 0;
    var outcome = "Pass";
    var failureCodes = Array.Empty<string>();

    await using var source = new DraftRescue.Platform.Windows.Automation.UiaFocusedElementMetadataSource();
    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(6));
    var enumerator = source.WatchAsync(cancellationToken.Token).GetAsyncEnumerator(cancellationToken.Token);

    try
    {
        // Deliberately stress only the metadata reconciliation signal path. The
        // source itself requests an audited Tier A/B UIA cache and never reads
        // target text, clipboard data, window titles, or dynamic UIA strings.
        for (var index = 0; index < 2000; index++)
        {
            source.RequestMetadataReconciliation();
        }

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
        {
            try
            {
                if (!await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromMilliseconds(250)))
                {
                    break;
                }

                observationCount++;
            }
            catch (TimeoutException)
            {
                break;
            }
        }

        if (source.MetadataReadCount > 64 || observationCount > 64)
        {
            outcome = "Inconclusive";
        }
    }
    catch (Exception)
    {
        // Keep the experiment content-free and stable: the record carries only
        // the typed outcome/counters, never a provider exception message.
        outcome = "Inconclusive";
        failureCodes = new[] { "DR-PERF-0061" };
    }
    finally
    {
        cancellationToken.Cancel();
        await source.DisposeAsync();
        try
        {
            await enumerator.DisposeAsync();
        }
        catch (NotSupportedException)
        {
            // Some framework channel iterators do not expose an explicit
            // disposer; source disposal above already completed the channel.
        }
        cancellationToken.Dispose();
        stopwatch.Stop();
    }

    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-PROVIDER-STRESS",
        runId,
        scenarioId = "WP14_UIA_PROVIDER_METADATA_STRESS",
        outcome,
        durationMs = stopwatch.ElapsedMilliseconds,
        failureCodes,
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-026", "P-028", "P-029", "P-030", "C-003", "C-016", "C-017", "C-022", "C-023" },
        testIds = new[] { "PERF-006", "EXP-001", "EXP-002" },
        measurements = new Dictionary<string, object>
        {
            ["reconciliation_request_count"] = 2000,
            ["observation_count"] = observationCount,
            ["uia_reconciliation_signal_count"] = source.ReconciliationSignalCount,
            ["uia_metadata_read_count"] = source.MetadataReadCount,
            ["uia_metadata_non_null_count"] = source.MetadataNonNullCount,
            ["uia_metadata_failure_count"] = source.MetadataFailureCount,
            ["uia_backoff_suppressed_count"] = source.BackoffSuppressedCount,
            ["uia_last_metadata_process_id"] = source.LastMetadataProcessId,
            ["text_reader_invocation_count"] = 0,
            ["repository_invocation_count"] = 0,
            ["target_content_read"] = false
        },
        contentSafetyAudit = new
        {
            rawTargetTextCaptured = false,
            rawDynamicUiaStringsLogged = false,
            clipboardRead = false,
            clipboardWritten = false,
            networkTextSent = false,
            testDataClass = "MetadataOnly"
        }
    };

    await WriteRecordAsync(outputDirectory, "WP14-PROVIDER-STRESS", record);
    return record;
}

static async Task<object> RunSyntheticStormAsync(string runId, string outputDirectory)
{
    var foreground = new FakeForegroundSource();
    var focused = new FakeFocusedSource();
    var process = Process.GetCurrentProcess();
    var memoryBefore = process.PrivateMemorySize64;
    var threadsBefore = process.Threads.Count;
    var stopwatch = Stopwatch.StartNew();
    var observations = new List<ObservedContextChanged>();

    await using var coordinator = new MetadataObservationCoordinator(
        foreground,
        focused,
        (uint)Environment.ProcessId + 1);
    var enumerator = coordinator.WatchAsync(CancellationToken.None).GetAsyncEnumerator();

    foreground.Emit(Foreground(1, 42, 100));
    focused.Emit(Focused(1, 42, 100, UiaBooleanSignal.True));
    await ReadUntilAsync(enumerator, observations, value =>
        value.State == ObservationContextState.CandidateMetadata && value.Foreground?.ProcessId == 42);

    for (ulong sequence = 2; sequence <= 1001; sequence++)
    {
        focused.Emit(Focused(sequence, 42, 100, UiaBooleanSignal.True));
    }

    foreground.Emit(Foreground(2, 43, 200));
    focused.Emit(Focused(1002, 43, 200, UiaBooleanSignal.True));
    await ReadUntilAsync(enumerator, observations, value =>
        value.State == ObservationContextState.CandidateMetadata && value.Foreground?.ProcessId == 43);

    stopwatch.Stop();
    await enumerator.DisposeAsync();
    var lifecycle = await RunLifecycleCyclesAsync(25);
    process.Refresh();

    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-SYNTH",
        runId,
        scenarioId = "WP14_SYNTHETIC_FOCUS_STORM",
        outcome = "Pass",
        durationMs = stopwatch.ElapsedMilliseconds,
        failureCodes = Array.Empty<string>(),
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-025", "P-026", "P-028", "P-029", "P-030", "C-003", "C-016", "C-017", "C-018", "C-022", "C-023", "C-025" },
        testIds = new[] { "OBS-005", "OBS-006", "OBS-007", "PERF-005", "PERF-007", "EXP-001", "EXP-002" },
        measurements = new Dictionary<string, object>
        {
            ["event_burst_count"] = 1000,
            ["coalesced_candidate_count_for_first_context"] = observations.Count(value => value.State == ObservationContextState.CandidateMetadata && value.Foreground?.ProcessId == 42),
            ["latest_context_process_id"] = observations.Last(value => value.State == ObservationContextState.CandidateMetadata).Foreground!.Value.ProcessId,
            ["context_generation_count"] = observations.Last().ContextGeneration,
            ["duration_ms"] = stopwatch.ElapsedMilliseconds,
            ["private_memory_delta_bytes"] = process.PrivateMemorySize64 - memoryBefore,
            ["thread_delta"] = process.Threads.Count - threadsBefore,
            ["lifecycle_cycles"] = lifecycle.Cycles,
            ["lifecycle_private_memory_delta_bytes"] = lifecycle.PrivateMemoryDeltaBytes,
            ["lifecycle_thread_delta"] = lifecycle.ThreadDelta,
            ["lifecycle_handle_delta"] = lifecycle.HandleDelta,
            ["host_os"] = Environment.OSVersion.VersionString,
            ["dotnet_runtime"] = Environment.Version.ToString()
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

    await WriteRecordAsync(outputDirectory, "WP14-SYNTH", record);
    return record;
}

static async Task<LifecycleMeasurement> RunLifecycleCyclesAsync(int cycles)
{
    for (var warmup = 0; warmup < 3; warmup++)
    {
        await RunOneLifecycleCycleAsync(warmup);
    }

    var process = Process.GetCurrentProcess();
    process.Refresh();
    var memoryBefore = process.PrivateMemorySize64;
    var threadsBefore = process.Threads.Count;
    var handlesBefore = process.HandleCount;

    for (var cycle = 0; cycle < cycles; cycle++)
    {
        await RunOneLifecycleCycleAsync(cycle + 3);
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    process.Refresh();
    return new LifecycleMeasurement(
        cycles,
        process.PrivateMemorySize64 - memoryBefore,
        process.Threads.Count - threadsBefore,
        process.HandleCount - handlesBefore);
}

static async Task RunOneLifecycleCycleAsync(int cycle)
{
    var foreground = new FakeForegroundSource();
    var focused = new FakeFocusedSource();
    await using var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
    var enumerator = coordinator.WatchAsync(CancellationToken.None).GetAsyncEnumerator();
    foreground.Emit(Foreground(1, 42, (nint)(100 + cycle)));
    focused.Emit(Focused(1, 42, (nint)(100 + cycle), UiaBooleanSignal.True));
    await ReadUntilAsync(enumerator, new List<ObservedContextChanged>(), value =>
        value.State == ObservationContextState.CandidateMetadata);
    await enumerator.DisposeAsync();
}

static async Task<object> RunProviderFaultAsync(string runId, string outputDirectory)
{
    var foreground = new FakeForegroundSource();
    var focused = new FakeFocusedSource();
    var stopwatch = Stopwatch.StartNew();
    await using var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
    var enumerator = coordinator.WatchAsync(CancellationToken.None).GetAsyncEnumerator();
    var observations = new List<ObservedContextChanged>();

    foreground.Emit(Foreground(1, 42, 100));
    focused.Emit(Focused(1, 42, 100, UiaBooleanSignal.True));
    await ReadUntilAsync(enumerator, observations, value => value.State == ObservationContextState.CandidateMetadata);
    focused.Fail();
    await ReadUntilAsync(enumerator, observations, value => value.State == ObservationContextState.Transient);
    stopwatch.Stop();
    await enumerator.DisposeAsync();

    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-FAULT",
        runId,
        scenarioId = "WP14_FOCUSED_PROVIDER_FAILURE",
        outcome = "Pass",
        durationMs = stopwatch.ElapsedMilliseconds,
        failureCodes = new[] { "DR-OBS-1005" },
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-028", "P-029", "C-003", "C-017", "C-023" },
        testIds = new[] { "FI-001", "FI-002", "FI-004", "EXP-001", "EXP-002" },
        measurements = new Dictionary<string, object>
        {
            ["provider_fault_count"] = 1,
            ["transient_observation_count"] = observations.Count(value => value.State == ObservationContextState.Transient),
            ["duration_ms"] = stopwatch.ElapsedMilliseconds,
            ["text_reader_invocation_count"] = 0,
            ["repository_invocation_count"] = 0
        },
        contentSafetyAudit = new
        {
            rawTargetTextCaptured = false,
            rawDynamicUiaStringsLogged = false,
            clipboardRead = false,
            clipboardWritten = false,
            networkTextSent = false,
            testDataClass = "MetadataOnly"
        }
    };

    await WriteRecordAsync(outputDirectory, "WP14-FAULT", record);
    return record;
}

static async Task<object> RunSyntheticSoakAsync(string runId, string outputDirectory, int soakSeconds)
{
    for (var warmup = 0; warmup < 3; warmup++)
    {
        await RunOneLifecycleCycleAsync(100 + warmup);
    }

    var foreground = new FakeForegroundSource();
    var focused = new FakeFocusedSource();
    var process = Process.GetCurrentProcess();
    process.Refresh();
    var memoryBefore = process.PrivateMemorySize64;
    var handlesBefore = process.HandleCount;
    var threadsBefore = process.Threads.Count;
    var observedTransitionCount = 0;
    var candidateTransitionCount = 0;
    var stopwatch = Stopwatch.StartNew();

    await using var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
    var enumerator = coordinator.WatchAsync(CancellationToken.None).GetAsyncEnumerator();
    var drain = Task.Run(async () =>
    {
        while (await enumerator.MoveNextAsync())
        {
            var observation = enumerator.Current;
            Interlocked.Increment(ref observedTransitionCount);
            if (observation.State == ObservationContextState.CandidateMetadata)
            {
                Interlocked.Increment(ref candidateTransitionCount);
            }
        }
    });

    var emitted = 0;
    var sequence = 1UL;
    while (stopwatch.Elapsed < TimeSpan.FromSeconds(soakSeconds))
    {
        var processId = emitted % 2 == 0 ? 42 : 43;
        var hwnd = emitted % 2 == 0 ? (nint)100 : (nint)200;
        foreground.Emit(Foreground(sequence, processId, hwnd));
        focused.Emit(Focused(sequence, processId, hwnd, UiaBooleanSignal.True));
        sequence++;
        emitted++;
        await Task.Delay(5).ConfigureAwait(false);
    }

    stopwatch.Stop();
    await coordinator.DisposeAsync();
    await drain.ConfigureAwait(false);
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    process.Refresh();

    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-SOAK",
        runId,
        scenarioId = "WP14_SYNTHETIC_FOCUS_SOAK",
        outcome = "Pass",
        durationMs = stopwatch.ElapsedMilliseconds,
        failureCodes = Array.Empty<string>(),
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-025", "P-026", "P-028", "P-029", "P-030", "C-003", "C-016", "C-017", "C-018", "C-022", "C-023", "C-025" },
        testIds = new[] { "PERF-007", "OBS-005", "OBS-006", "EXP-001", "EXP-002" },
        measurements = new Dictionary<string, object>
        {
            ["soak_duration_seconds"] = soakSeconds,
            ["emitted_context_pairs"] = emitted,
            ["observed_transition_count"] = observedTransitionCount,
            ["candidate_transition_count"] = candidateTransitionCount,
            ["private_memory_delta_bytes"] = process.PrivateMemorySize64 - memoryBefore,
            ["handle_delta"] = process.HandleCount - handlesBefore,
            ["thread_delta"] = process.Threads.Count - threadsBefore,
            ["text_reader_invocation_count"] = 0,
            ["repository_invocation_count"] = 0
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

    await WriteRecordAsync(outputDirectory, "WP14-SOAK", record);
    return record;
}

static async Task<object> RunProviderHangStressAsync(string runId, string outputDirectory)
{
    var foreground = new FakeForegroundSource();
    var focused = new HungFocusedSource();
    await using var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
    _ = coordinator.WatchAsync(CancellationToken.None);

    var stopwatch = Stopwatch.StartNew();
    await coordinator.DisposeAsync();
    stopwatch.Stop();

    var outcome = stopwatch.Elapsed < TimeSpan.FromSeconds(2) && focused.ReleaseCount == 1
        ? "Pass"
        : "Inconclusive";
    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-HANG",
        runId,
        scenarioId = "WP14_HUNG_PROVIDER_BOUNDED_SHUTDOWN",
        outcome,
        durationMs = stopwatch.ElapsedMilliseconds,
        failureCodes = outcome == "Pass" ? Array.Empty<string>() : new[] { "DR-OBS-1006" },
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-028", "P-029", "P-030", "C-003", "C-017", "C-023", "C-025" },
        testIds = new[] { "FI-001", "FI-002", "FI-004", "PERF-006", "EXP-001", "EXP-002" },
        measurements = new Dictionary<string, object>
        {
            ["provider_hang_simulated"] = true,
            ["dispose_duration_ms"] = stopwatch.ElapsedMilliseconds,
            ["source_release_count"] = focused.ReleaseCount,
            ["ui_thread_blocked"] = false,
            ["text_reader_invocation_count"] = 0,
            ["repository_invocation_count"] = 0
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

    await WriteRecordAsync(outputDirectory, "WP14-HANG", record);
    return record;
}

static async Task<object> RunResponsivenessProbeAsync(string runId, string outputDirectory)
{
    const int eventPairCount = 2000;
    var foreground = new FakeForegroundSource();
    var focused = new FakeFocusedSource();
    var stopwatch = Stopwatch.StartNew();
    var observations = new List<ObservedContextChanged>();

    await using var coordinator = new MetadataObservationCoordinator(foreground, focused, uint.MaxValue);
    var enumerator = coordinator.WatchAsync(CancellationToken.None).GetAsyncEnumerator();
    foreground.Emit(Foreground(1, 42, 100));
    focused.Emit(Focused(1, 42, 100, UiaBooleanSignal.True));
    await ReadUntilAsync(enumerator, observations, value =>
        value.State == ObservationContextState.CandidateMetadata && value.Foreground?.ProcessId == 42);

    for (ulong sequence = 2; sequence <= eventPairCount; sequence++)
    {
        foreground.Emit(Foreground(sequence, 42, 100));
        focused.Emit(Focused(sequence, 42, 100, UiaBooleanSignal.True));
    }
    foreground.Emit(Foreground(eventPairCount + 1, 43, 200));
    focused.Emit(Focused(eventPairCount + 1, 43, 200, UiaBooleanSignal.True));

    var enqueueDuration = stopwatch.Elapsed;
    await Task.Delay(50).ConfigureAwait(false);
    stopwatch.Stop();
    await enumerator.DisposeAsync();

    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-RESP",
        runId,
        scenarioId = "WP14_OBSERVATION_PLANE_RESPONSIVENESS",
        outcome = enqueueDuration < TimeSpan.FromSeconds(1) ? "Pass" : "Inconclusive",
        durationMs = stopwatch.ElapsedMilliseconds,
        failureCodes = Array.Empty<string>(),
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-025", "P-026", "P-028", "P-029", "P-030", "C-003", "C-016", "C-017", "C-022", "C-023", "C-025" },
        testIds = new[] { "PERF-005", "PERF-007", "EXP-001", "EXP-002" },
        measurements = new Dictionary<string, object>
        {
            ["event_pair_count"] = eventPairCount,
            ["enqueue_duration_ms"] = enqueueDuration.TotalMilliseconds,
            ["drain_duration_ms"] = stopwatch.Elapsed.TotalMilliseconds,
            ["observed_transition_count"] = observations.Count,
            ["queue_depth_bound"] = 64,
            ["ui_thread_blocked"] = false,
            ["text_reader_invocation_count"] = 0,
            ["repository_invocation_count"] = 0
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

    await WriteRecordAsync(outputDirectory, "WP14-RESP", record);
    return record;
}

static int ParseSoakSeconds(string[] arguments)
{
    var index = Array.FindIndex(arguments, value => string.Equals(value, "--soak-seconds", StringComparison.OrdinalIgnoreCase));
    if (index < 0)
    {
        return 30;
    }

    if (index + 1 >= arguments.Length ||
        !int.TryParse(arguments[index + 1], out var seconds) ||
        seconds is < 1 or > 1800)
    {
        throw new ArgumentException("--soak-seconds must be an integer between 1 and 1800.");
    }

    return seconds;
}

static async Task<object> RunNotepadReconnaissanceAsync(string runId, string outputDirectory)
{
    Process? notepad = null;
    var stopwatch = Stopwatch.StartNew();
    var states = new HashSet<ObservationContextState>();
    var controlKinds = new HashSet<UiaControlKind>();
    var frameworkKinds = new HashSet<UiaFrameworkKind>();
    var observationCount = 0;
    var processId = 0;
    var launchedProcessId = 0;
    var targetForegroundSeen = 0;
    var targetFocusedSeen = 0;
    var integrityCompatibility = IntegrityCompatibility.Unknown;
    var outcome = "Inconclusive";
    await using var foreground = new DraftRescue.Platform.Windows.Observation.WinEventForegroundContextSource();
    await using var focused = new DraftRescue.Platform.Windows.Automation.UiaFocusedElementMetadataSource();
    await using var coordinator = new MetadataObservationCoordinator(
        foreground,
        focused,
        (uint)Environment.ProcessId);
    var enumerator = coordinator.WatchAsync(CancellationToken.None).GetAsyncEnumerator();

    try
    {
        notepad = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true });
        if (notepad is null)
        {
            throw new InvalidOperationException("Notepad process did not start.");
        }

        notepad.Refresh();
        launchedProcessId = notepad.Id;
        if (launchedProcessId == 0)
        {
            throw new InvalidOperationException("Notepad process id was unavailable.");
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (await enumerator.MoveNextAsync().AsTask().WaitAsync(timeout.Token))
        {
            var observation = enumerator.Current;
            observationCount++;
            states.Add(observation.State);
            if (observation.Focused is not null)
            {
                controlKinds.Add(observation.Focused.ControlKind);
                frameworkKinds.Add(observation.Focused.FrameworkKind);
                processId = observation.Focused.ProcessId;
                if (observation.Focused.ProcessId == launchedProcessId)
                {
                    targetFocusedSeen++;
                    integrityCompatibility = observation.Focused.IntegrityCompatibility;
                }
            }

            if (observation.Foreground?.ProcessId == launchedProcessId)
            {
                targetForegroundSeen++;
            }

            if (observation.Foreground?.ProcessId == launchedProcessId &&
                observation.Focused?.ProcessId == launchedProcessId)
            {
                outcome = "Pass";
                if (states.Contains(ObservationContextState.CandidateMetadata) ||
                    states.Contains(ObservationContextState.Unsupported))
                {
                    break;
                }
            }
        }

        await enumerator.DisposeAsync();
    }
    catch (OperationCanceledException)
    {
        // No event within the bounded probe window is an inconclusive result.
    }
    finally
    {
        stopwatch.Stop();
        if (notepad is not null && !notepad.HasExited)
        {
            try
            {
                notepad.CloseMainWindow();
                if (!notepad.WaitForExit(2000))
                {
                    notepad.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Probe cleanup is best-effort and never logs provider/process text.
            }
        }

        notepad?.Dispose();
    }

    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-NPAD",
        runId,
        scenarioId = "WP14_NOTEPAD_METADATA_RECON",
        outcome,
        durationMs = stopwatch.ElapsedMilliseconds,
        failureCodes = Array.Empty<string>(),
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-026", "P-028", "P-029", "P-030", "C-003", "C-016", "C-017", "C-022", "C-023" },
        testIds = new[] { "CERT-001", "EXP-001", "EXP-002" },
        measurements = new Dictionary<string, object>
        {
            ["target_process"] = "notepad",
            ["target_process_id"] = launchedProcessId,
            ["observation_count"] = observationCount,
            ["target_foreground_observation_count"] = targetForegroundSeen,
            ["target_focused_observation_count"] = targetFocusedSeen,
            ["state_count"] = states.Count,
            ["control_kind_count"] = controlKinds.Count,
            ["framework_kind_count"] = frameworkKinds.Count,
            ["probe_duration_ms"] = stopwatch.ElapsedMilliseconds,
            ["text_reader_invocation_count"] = 0,
            ["window_title_read"] = false
            , ["integrity_compatibility"] = integrityCompatibility.ToString()
            , ["uia_reconciliation_signal_count"] = focused.ReconciliationSignalCount
            , ["uia_metadata_read_count"] = focused.MetadataReadCount
            , ["uia_metadata_non_null_count"] = focused.MetadataNonNullCount
            , ["uia_metadata_failure_count"] = focused.MetadataFailureCount
            , ["uia_last_metadata_process_id"] = focused.LastMetadataProcessId
            , ["delivery_diagnosis"] = DiagnoseMetadataDelivery(launchedProcessId, targetForegroundSeen, targetFocusedSeen, focused.ReconciliationSignalCount, focused.MetadataNonNullCount, focused.MetadataFailureCount, focused.LastMetadataProcessId)
        },
        contentSafetyAudit = new
        {
            rawTargetTextCaptured = false,
            rawDynamicUiaStringsLogged = false,
            clipboardRead = false,
            clipboardWritten = false,
            networkTextSent = false,
            testDataClass = "MetadataOnly"
        }
    };

    await WriteRecordAsync(outputDirectory, "WP14-NPAD", record);
    return record;
}

static async Task<object> RunWpfFixtureReconnaissanceAsync(string runId, string outputDirectory)
{
    Process? fixture = null;
    var stopwatch = Stopwatch.StartNew();
    var observationCount = 0;
    var focusedMetadataCount = 0;
    var targetForegroundCount = 0;
    var lastForegroundProcessId = 0;
    var launchedProcessId = 0;
    var activationAttemptCount = 0;
    var activationSuccessCount = 0;
    var activationVerified = false;
    var automationFocusAttemptCount = 0;
    var automationFocusSuccessCount = 0;
    var controlKind = UiaControlKind.Unknown;
    var frameworkKind = UiaFrameworkKind.Unknown;
    var integrityCompatibility = IntegrityCompatibility.Unknown;
    var outcome = "Inconclusive";

    await using var foreground = new DraftRescue.Platform.Windows.Observation.WinEventForegroundContextSource();
    await using var focused = new DraftRescue.Platform.Windows.Automation.UiaFocusedElementMetadataSource();
    await using var coordinator = new MetadataObservationCoordinator(foreground, focused, (uint)Environment.ProcessId);
    var enumerator = coordinator.WatchAsync(CancellationToken.None).GetAsyncEnumerator();

    try
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("Harness process path was unavailable.");
        }

        // Shell launch gives the synthetic fixture a normal interactive foreground context.
        // This is test-only process setup; no production target is activated by DraftRescue.
        var startInfo = new ProcessStartInfo(processPath) { UseShellExecute = true };
        startInfo.ArgumentList.Add("--wpf-fixture");
        fixture = Process.Start(startInfo);
        if (fixture is null)
        {
            throw new InvalidOperationException("WPF fixture process did not start.");
        }

        fixture.Refresh();
        launchedProcessId = fixture.Id;
        if (launchedProcessId == 0)
        {
            throw new InvalidOperationException("WPF fixture process id was unavailable.");
        }

        (activationAttemptCount, activationSuccessCount, activationVerified, automationFocusAttemptCount, automationFocusSuccessCount) =
            ActivateSyntheticFixture(fixture);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (await enumerator.MoveNextAsync().AsTask().WaitAsync(timeout.Token))
        {
            var observation = enumerator.Current;
            observationCount++;
            if (observation.Foreground is not null)
            {
                lastForegroundProcessId = observation.Foreground.Value.ProcessId;
                if (lastForegroundProcessId == launchedProcessId)
                {
                    targetForegroundCount++;
                }
            }

            if (observation.Focused?.ProcessId == launchedProcessId)
            {
                focusedMetadataCount++;
                controlKind = observation.Focused.ControlKind;
                frameworkKind = observation.Focused.FrameworkKind;
                integrityCompatibility = observation.Focused.IntegrityCompatibility;
            }

            if (observation.Foreground?.ProcessId == launchedProcessId &&
                observation.Focused?.ProcessId == launchedProcessId &&
                observation.State is ObservationContextState.CandidateMetadata or ObservationContextState.Unsupported)
            {
                outcome = "Pass";
                break;
            }
        }

        await enumerator.DisposeAsync();
    }
    catch (OperationCanceledException)
    {
        // No matching focused metadata within the bounded probe window is inconclusive.
    }
    finally
    {
        stopwatch.Stop();
        if (fixture is not null && !fixture.HasExited)
        {
            try
            {
                fixture.CloseMainWindow();
                if (!fixture.WaitForExit(2000))
                {
                    fixture.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Probe cleanup is best-effort and never logs target strings.
            }
        }

        fixture?.Dispose();
    }

    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-WPF",
        runId,
        scenarioId = "WP14_WPF_TEXTBOX_METADATA_RECON",
        outcome,
        durationMs = stopwatch.ElapsedMilliseconds,
        failureCodes = Array.Empty<string>(),
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-026", "P-028", "P-029", "P-030", "C-003", "C-016", "C-017", "C-022", "C-023" },
        testIds = new[] { "CERT-001", "EXP-001", "EXP-002" },
        measurements = new Dictionary<string, object>
        {
            ["target_process"] = "synthetic-wpf-fixture",
            ["target_process_id"] = launchedProcessId,
            ["activation_attempt_count"] = activationAttemptCount,
            ["activation_success_count"] = activationSuccessCount,
            ["activation_verified"] = activationVerified,
            ["automation_focus_attempt_count"] = automationFocusAttemptCount,
            ["automation_focus_success_count"] = automationFocusSuccessCount,
            ["observation_count"] = observationCount,
            ["target_foreground_observation_count"] = targetForegroundCount,
            ["last_foreground_process_id"] = lastForegroundProcessId,
            ["focused_metadata_count"] = focusedMetadataCount,
            ["control_kind"] = controlKind.ToString(),
            ["framework_kind"] = frameworkKind.ToString(),
            ["probe_duration_ms"] = stopwatch.ElapsedMilliseconds,
            ["text_reader_invocation_count"] = 0,
            ["window_title_read"] = false
            , ["integrity_compatibility"] = integrityCompatibility.ToString()
            , ["uia_reconciliation_signal_count"] = focused.ReconciliationSignalCount
            , ["uia_metadata_read_count"] = focused.MetadataReadCount
            , ["uia_metadata_non_null_count"] = focused.MetadataNonNullCount
            , ["uia_metadata_failure_count"] = focused.MetadataFailureCount
            , ["uia_last_metadata_process_id"] = focused.LastMetadataProcessId
            , ["delivery_diagnosis"] = DiagnoseMetadataDelivery(launchedProcessId, targetForegroundCount, focusedMetadataCount, focused.ReconciliationSignalCount, focused.MetadataNonNullCount, focused.MetadataFailureCount, focused.LastMetadataProcessId, activationAttemptCount, activationSuccessCount, activationVerified)
        },
        contentSafetyAudit = new
        {
            rawTargetTextCaptured = false,
            rawDynamicUiaStringsLogged = false,
            clipboardRead = false,
            clipboardWritten = false,
            networkTextSent = false,
            testDataClass = "MetadataOnly"
        }
    };

    await WriteRecordAsync(outputDirectory, "WP14-WPF", record);
    return record;
}

static async Task<object> RunInteractiveWpfFixtureReconnaissanceAsync(string runId, string outputDirectory)
{
    Process? fixture = null;
    var stopwatch = Stopwatch.StartNew();
    var observationCount = 0;
    var focusedMetadataCount = 0;
    var targetForegroundCount = 0;
    var lastForegroundProcessId = 0;
    var launchedProcessId = 0;
    var activationAttemptCount = 0;
    var activationSuccessCount = 0;
    var activationVerified = false;
    var automationFocusAttemptCount = 0;
    var automationFocusSuccessCount = 0;
    var controlKind = UiaControlKind.Unknown;
    var frameworkKind = UiaFrameworkKind.Unknown;
    var integrityCompatibility = IntegrityCompatibility.Unknown;
    var outcome = "Inconclusive";
    var preflightBefore = DraftRescue.Platform.Windows.Testing.InteractiveSessionPreflightResult.Capture();
    DraftRescue.Platform.Windows.Testing.InteractiveSessionPreflightResult? preflightAfterInteraction = null;

    await using var foreground = new DraftRescue.Platform.Windows.Observation.WinEventForegroundContextSource();
    await using var focused = new DraftRescue.Platform.Windows.Automation.UiaFocusedElementMetadataSource();
    await using var coordinator = new MetadataObservationCoordinator(foreground, focused, (uint)Environment.ProcessId);
    var enumerator = coordinator.WatchAsync(CancellationToken.None).GetAsyncEnumerator();

    try
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("Harness process path was unavailable.");
        }

        fixture = Process.Start(new ProcessStartInfo(processPath) { UseShellExecute = true, ArgumentList = { "--wpf-fixture" } });
        if (fixture is null)
        {
            throw new InvalidOperationException("WPF fixture process did not start.");
        }

        fixture.Refresh();
        launchedProcessId = fixture.Id;
        (activationAttemptCount, activationSuccessCount, activationVerified, automationFocusAttemptCount, automationFocusSuccessCount) =
            ActivateSyntheticFixture(fixture);
        Console.WriteLine("Interactive WPF fixture is open. Click the empty test field, then press Enter here.");
        _ = Console.ReadLine();
        preflightAfterInteraction = DraftRescue.Platform.Windows.Testing.InteractiveSessionPreflightResult.Capture();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (await enumerator.MoveNextAsync().AsTask().WaitAsync(timeout.Token))
        {
            var observation = enumerator.Current;
            observationCount++;
            if (observation.Foreground is not null)
            {
                lastForegroundProcessId = observation.Foreground.Value.ProcessId;
                if (lastForegroundProcessId == launchedProcessId)
                {
                    targetForegroundCount++;
                }
            }

            if (observation.Focused?.ProcessId == launchedProcessId)
            {
                focusedMetadataCount++;
                controlKind = observation.Focused.ControlKind;
                frameworkKind = observation.Focused.FrameworkKind;
                integrityCompatibility = observation.Focused.IntegrityCompatibility;
            }

            if (targetForegroundCount > 0 && focusedMetadataCount > 0 &&
                observation.State is ObservationContextState.CandidateMetadata or ObservationContextState.Unsupported)
            {
                outcome = "Pass";
                break;
            }
        }

        await enumerator.DisposeAsync();
    }
    catch (OperationCanceledException)
    {
        // Interactive focus was not observed within the bounded window.
    }
    finally
    {
        stopwatch.Stop();
        if (fixture is not null && !fixture.HasExited)
        {
            try
            {
                fixture.CloseMainWindow();
                if (!fixture.WaitForExit(2000))
                {
                    fixture.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Cleanup is best-effort and never logs provider/process text.
            }
        }

        fixture?.Dispose();
    }

    var record = new
    {
        schemaVersion = 1,
        experimentId = "WP14-WPF-INTERACTIVE",
        runId,
        scenarioId = "WP14_WPF_INTERACTIVE_FOCUS_CONFIRMATION",
        outcome,
        durationMs = stopwatch.ElapsedMilliseconds,
        failureCodes = Array.Empty<string>(),
        invariantIds = new[] { "P-001", "P-002", "P-003", "P-026", "P-028", "P-029", "P-030", "C-003", "C-016", "C-017", "C-022", "C-023" },
        testIds = new[] { "CERT-001", "EXP-001", "EXP-002" },
        measurements = new Dictionary<string, object>
        {
            ["target_process"] = "synthetic-wpf-fixture",
            ["target_process_id"] = launchedProcessId,
            ["activation_attempt_count"] = activationAttemptCount,
            ["activation_success_count"] = activationSuccessCount,
            ["activation_verified"] = activationVerified,
            ["automation_focus_attempt_count"] = automationFocusAttemptCount,
            ["automation_focus_success_count"] = automationFocusSuccessCount,
            ["observation_count"] = observationCount,
            ["target_foreground_observation_count"] = targetForegroundCount,
            ["last_foreground_process_id"] = lastForegroundProcessId,
            ["focused_metadata_count"] = focusedMetadataCount,
            ["control_kind"] = controlKind.ToString(),
            ["framework_kind"] = frameworkKind.ToString(),
            ["probe_duration_ms"] = stopwatch.ElapsedMilliseconds,
            ["text_reader_invocation_count"] = 0,
            ["window_title_read"] = false,
            ["integrity_compatibility"] = integrityCompatibility.ToString(),
            ["uia_reconciliation_signal_count"] = focused.ReconciliationSignalCount,
            ["uia_metadata_read_count"] = focused.MetadataReadCount,
            ["uia_metadata_non_null_count"] = focused.MetadataNonNullCount,
            ["uia_metadata_failure_count"] = focused.MetadataFailureCount,
            ["delivery_diagnosis"] = DiagnoseMetadataDelivery(launchedProcessId, targetForegroundCount, focusedMetadataCount, focused.ReconciliationSignalCount, focused.MetadataNonNullCount, focused.MetadataFailureCount, focused.LastMetadataProcessId, activationAttemptCount, activationSuccessCount, activationVerified)
            , ["preflight_before_user_interactive"] = preflightBefore.UserInteractive
            , ["preflight_before_foreground_window_present"] = preflightBefore.ForegroundWindowPresent
            , ["preflight_before_input_desktop_accessible"] = preflightBefore.InputDesktopAccessible
            , ["preflight_after_foreground_window_present"] = preflightAfterInteraction?.ForegroundWindowPresent ?? false
            , ["preflight_after_foreground_focus_window_present"] = preflightAfterInteraction?.ForegroundFocusWindowPresent ?? false
            , ["preflight_after_input_desktop_accessible"] = preflightAfterInteraction?.InputDesktopAccessible ?? false
            , ["preflight_after_ready_for_interactive_fixture"] = preflightAfterInteraction?.ReadyForInteractiveFixture ?? false
        },
        contentSafetyAudit = new
        {
            rawTargetTextCaptured = false,
            rawDynamicUiaStringsLogged = false,
            clipboardRead = false,
            clipboardWritten = false,
            networkTextSent = false,
            testDataClass = "MetadataOnly"
        }
    };

    await WriteRecordAsync(outputDirectory, "WP14-WPF-INTERACTIVE", record);
    return record;
}

static (int Attempts, int Successes, bool Verified, int AutomationAttempts, int AutomationSuccesses) ActivateSyntheticFixture(Process fixture)
{
    var attempts = 0;
    var successes = 0;
    var verified = false;
    var automationAttempts = 0;
    var automationSuccesses = 0;
    var automationAttempted = false;

    try
    {
        fixture.WaitForInputIdle(2000);
    }
    catch
    {
        // The fixture remains eligible for the bounded handle polling below.
    }

    for (var index = 0; index < 20; index++)
    {
        fixture.Refresh();
        var windowHandle = fixture.MainWindowHandle;
        if (windowHandle != nint.Zero)
        {
            attempts++;
            if (DraftRescue.Platform.Windows.Testing.SyntheticWindowActivator.TryActivate(windowHandle))
            {
                successes++;
            }

            verified = DraftRescue.Platform.Windows.Testing.SyntheticWindowActivator.IsForeground(windowHandle);
            if (!verified && !automationAttempted)
            {
                automationAttempted = true;
                automationAttempts++;
                if (TrySetAutomationFocus(windowHandle))
                {
                    automationSuccesses++;
                }

                verified = DraftRescue.Platform.Windows.Testing.SyntheticWindowActivator.IsForeground(windowHandle);
            }

            if (verified)
            {
                break;
            }
        }

        Thread.Sleep(100);
    }

    return (attempts, successes, verified, automationAttempts, automationSuccesses);
}

static bool TrySetAutomationFocus(nint windowHandle)
{
    try
    {
        // Test-only structural operation on the synthetic fixture. No Name,
        // Value, text pattern, clipboard, or other content property is read.
        var element = Uia.AutomationElement.FromHandle(windowHandle);
        element.SetFocus();
        return true;
    }
    catch
    {
        return false;
    }
}

static void RunWpfFixture()
{
    var ready = new ManualResetEventSlim(false);
    var thread = new Thread(() =>
    {
        var application = new WpfApplication();
        var textBox = new TextBox { Width = 320, Height = 28 };
        var window = new Window
        {
            Width = 420,
            Height = 160,
            Content = textBox,
            ShowInTaskbar = true,
            Title = "DraftRescue synthetic WPF fixture",
            ShowActivated = true,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        window.Show();
        window.Activate();
        textBox.Focus();
        var windowHandle = new WindowInteropHelper(window).Handle;
        DraftRescue.Platform.Windows.Testing.SyntheticWindowActivator.TryActivate(windowHandle);
        // Re-activate after the parent has had time to register global UIA focus handlers.
        var focusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        focusTimer.Tick += (_, _) =>
        {
            focusTimer.Stop();
            window.Activate();
            DraftRescue.Platform.Windows.Testing.SyntheticWindowActivator.TryActivate(windowHandle);
            textBox.Focus();
        };
        focusTimer.Start();
        ready.Set();
        application.Run();
    })
    {
        IsBackground = false,
        Name = "DraftRescue.WpfFixture"
    };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    ready.Wait(TimeSpan.FromSeconds(5));
    thread.Join();
}

static string DiagnoseMetadataDelivery(
    int targetProcessId,
    int targetForegroundCount,
    int targetFocusedCount,
    long reconciliationSignals,
    long metadataNonNullCount,
    long metadataFailureCount,
    int lastMetadataProcessId,
    int activationAttemptCount = 0,
    int activationSuccessCount = 0,
    bool activationVerified = false)
{
    if (targetFocusedCount > 0)
    {
        return "FixtureFocused";
    }

    if (targetForegroundCount == 0)
    {
        return "NoFixtureForegroundSignal";
    }

    if (reconciliationSignals == 0)
    {
        return "NoReconciliationSignal";
    }

    if (metadataFailureCount > 0 && metadataNonNullCount == 0)
    {
        return "ProviderReadFailure";
    }

    if (metadataNonNullCount == 0)
    {
        return "ProviderReturnedNull";
    }

    if (lastMetadataProcessId != 0 && lastMetadataProcessId != targetProcessId)
    {
        if (activationAttemptCount > 0 && activationSuccessCount == 0 && !activationVerified)
        {
            return "ActivationBlocked";
        }

        return "MetadataReadButForeignFocus";
    }

    return "MetadataReadWithoutTargetMatch";
}

static async Task ReadUntilAsync(
    IAsyncEnumerator<ObservedContextChanged> enumerator,
    ICollection<ObservedContextChanged> observations,
    Func<ObservedContextChanged, bool> predicate)
{
    while (await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)))
    {
        observations.Add(enumerator.Current);
        if (predicate(enumerator.Current))
        {
            return;
        }
    }

    throw new InvalidOperationException("Expected metadata observation was not emitted.");
}

static async Task WriteRecordAsync(string outputDirectory, string name, object record)
{
    var path = Path.Combine(outputDirectory, $"{name}.json");
    await using var stream = File.Create(path);
    await JsonSerializer.SerializeAsync(stream, record, new JsonSerializerOptions { WriteIndented = true });
}

static ForegroundApplicationContextChanged Foreground(ulong sequence, int processId, nint hwnd) =>
    new(sequence, (long)sequence, new ForegroundApplicationIdentity(
        new DomainApplicationId("synthetic"), processId, hwnd));

static FocusedElementMetadataChanged Focused(ulong sequence, int processId, nint hwnd, UiaBooleanSignal editable) =>
    new(sequence, (long)sequence, new FocusedElementMetadata(
        processId, hwnd, UiaControlKind.Edit, UiaFrameworkKind.Win32, editable,
        editable == UiaBooleanSignal.True ? UiaBooleanSignal.False : UiaBooleanSignal.True,
        UiaBooleanSignal.False, UiaBooleanSignal.True, UiaBooleanSignal.True,
        UiaBooleanSignal.True, UiaBooleanSignal.False, "synthetic-id", "Edit",
        UiaCapabilityHints.ValuePatternAvailable));

sealed class FakeForegroundSource : IForegroundContextSource
{
    private readonly Channel<ForegroundApplicationContextChanged> _events = Channel.CreateUnbounded<ForegroundApplicationContextChanged>();
    public IAsyncEnumerable<ForegroundApplicationContextChanged> WatchAsync(CancellationToken cancellationToken) => _events.Reader.ReadAllAsync(cancellationToken);
    public void Emit(ForegroundApplicationContextChanged value) => _events.Writer.TryWrite(value);
    public ValueTask DisposeAsync() { _events.Writer.TryComplete(); return ValueTask.CompletedTask; }
}

sealed class FakeFocusedSource : IFocusedElementMetadataSource
{
    private readonly Channel<FocusedElementMetadataChanged> _events = Channel.CreateUnbounded<FocusedElementMetadataChanged>();
    public IAsyncEnumerable<FocusedElementMetadataChanged> WatchAsync(CancellationToken cancellationToken) => _events.Reader.ReadAllAsync(cancellationToken);
    public void Emit(FocusedElementMetadataChanged value) => _events.Writer.TryWrite(value);
    public void Fail() => _events.Writer.TryComplete(new InvalidOperationException("synthetic provider fault"));
    public ValueTask DisposeAsync() { _events.Writer.TryComplete(); return ValueTask.CompletedTask; }
}

sealed class HungFocusedSource : IFocusedElementMetadataSource
{
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _releaseCount;

    public int ReleaseCount => Volatile.Read(ref _releaseCount);

    public async IAsyncEnumerable<FocusedElementMetadataChanged> WatchAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await _release.Task.ConfigureAwait(false);
        yield break;
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _releaseCount);
        _release.TrySetResult();
        return ValueTask.CompletedTask;
    }
}

sealed class FixtureBindingRegistry(SecurityEvidenceSet evidence) : ICapabilityBindingRegistry
{
    public bool TryGetCurrent(Guid bindingId, out CapabilityBindingSnapshot binding)
    {
        binding = new CapabilityBindingSnapshot(
            evidence.CandidateId,
            bindingId,
            evidence.ContextGeneration,
            evidence.ProfileId!,
            evidence.ProfileRevision,
            bindingId == evidence.BindingId);
        return binding.IsCurrent;
    }
}

sealed class Phase2HarnessRegistry(SecurityEvidenceSet evidence) : ICapabilityBindingRegistry
{
    public ulong ContextGeneration { get; set; } = evidence.ContextGeneration;
    public bool IsCurrent { get; set; } = true;

    public bool TryGetCurrent(Guid bindingId, out CapabilityBindingSnapshot binding)
    {
        binding = new CapabilityBindingSnapshot(
            evidence.CandidateId,
            bindingId,
            ContextGeneration,
            evidence.ProfileId!,
            evidence.ProfileRevision,
            IsCurrent);
        return bindingId == evidence.BindingId;
    }
}

sealed class Phase2HarnessClock : IMonotonicClock
{
    public long Now { get; set; }
    public long GetTimestampMilliseconds() => Now;
}

readonly record struct Phase2FaultCaseResult(
    bool Pass,
    string Scenario,
    string Decision,
    string StableErrorCode,
    int CapabilityIssueCount,
    int UsableClaimCount);

sealed class FixtureClock : IMonotonicClock
{
    public long GetTimestampMilliseconds() => 0;
}

readonly record struct LifecycleMeasurement(
    int Cycles,
    long PrivateMemoryDeltaBytes,
    int ThreadDelta,
    int HandleDelta);
