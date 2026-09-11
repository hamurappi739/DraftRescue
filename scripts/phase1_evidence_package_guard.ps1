param(
    [string]$OutputDirectory = 'artifacts\phase1-evidence-package-guard'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

function Read-JsonRecord([string]$RelativePath) {
    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing evidence record: $RelativePath"
    }

    try {
        return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    }
    catch {
        throw "Malformed evidence record: $RelativePath"
    }
}

function Assert-SafeRecord([object]$Record, [string]$Label) {
    if ($Record.schemaVersion -ne 1) { throw "Unsupported schema: $Label" }
    $audit = $Record.contentSafetyAudit
    if ($null -eq $audit -or
        $audit.rawTargetTextCaptured -ne $false -or
        $audit.rawDynamicUiaStringsLogged -ne $false -or
        $audit.clipboardRead -ne $false -or
        $audit.clipboardWritten -ne $false -or
        $audit.networkTextSent -ne $false) {
        throw "Content-safety audit failed: $Label"
    }
}

$recordPaths = @(
    'artifacts\phase1\WP14-SYNTH.json',
    'artifacts\phase1\WP14-FAULT.json',
    'artifacts\phase1\WP14-SOAK.json',
    'artifacts\phase1\WP14-WPF.json',
    'artifacts\phase1-desktop-ui\WP14-UI.json',
    'artifacts\phase1-hang-evidence\WP14-HANG.json',
    'artifacts\phase1-long\WP14-SOAK.json',
    'artifacts\phase1-provider-stress\WP14-PROVIDER-STRESS.json'
)

$records = @{}
foreach ($relativePath in $recordPaths) {
    $record = Read-JsonRecord $relativePath
    Assert-SafeRecord $record $relativePath
    $records[$relativePath] = $record
}

$stress = $records['artifacts\phase1-provider-stress\WP14-PROVIDER-STRESS.json']
if ($stress.outcome -ne 'Pass' -or
    [int64]$stress.measurements.reconciliation_request_count -lt 2000 -or
    [int64]$stress.measurements.uia_metadata_read_count -gt 64 -or
    [int64]$stress.measurements.text_reader_invocation_count -ne 0 -or
    [int64]$stress.measurements.repository_invocation_count -ne 0 -or
    $stress.measurements.target_content_read -ne $false) {
    throw 'Provider-stress evidence is incomplete or unsafe.'
}

$wpfCandidates = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'artifacts') -Directory -Filter 'phase1-wpf-repeat-*' |
    ForEach-Object {
        $candidatePath = Join-Path $_.FullName 'WP14-WPF-REPEAT-AUTO.json'
        if (-not (Test-Path -LiteralPath $candidatePath)) { return }
        try {
            $candidate = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json
            Assert-SafeRecord $candidate $candidatePath
            [pscustomobject]@{ Path = $candidatePath; Record = $candidate; LastWriteUtc = (Get-Item -LiteralPath $candidatePath).LastWriteTimeUtc }
        }
        catch {
            # Invalid candidates are ignored here; the formal exit gate remains
            # responsible for reporting a missing/unsafe selected summary.
        }
    } | Sort-Object LastWriteUtc -Descending
$wpf = $wpfCandidates | Select-Object -First 1
if ($null -eq $wpf) { throw 'No valid automated WPF repeat summary found.' }

$repeatCount = [int]$wpf.Record.measurements.repeat_count
$attempts = @($wpf.Record.measurements.attempts)
if ($repeatCount -lt 5 -or $attempts.Count -ne $repeatCount) {
    throw 'Automated WPF repeat summary is empty or incomplete.'
}

$activationBlocked = @($attempts | Where-Object { [string]$_.delivery_diagnosis -eq 'ActivationBlocked' }).Count
$activationAttempts = [int64](@($attempts | ForEach-Object { [int]$_.activation_attempt_count } | Measure-Object -Sum).Sum)
$activationSuccesses = [int64](@($attempts | ForEach-Object { [int]$_.activation_success_count } | Measure-Object -Sum).Sum)
$focusedCount = [int64](@($attempts | ForEach-Object { [int]$_.focused_metadata_count } | Measure-Object -Sum).Sum)
if ($activationAttempts -gt 0 -and $activationSuccesses -eq 0 -and $focusedCount -eq 0 -and $activationBlocked -ne $repeatCount) {
    throw 'WPF activation telemetry contradicts the delivery diagnosis.'
}

$interactiveReview = $null
$interactiveReviewPath = Join-Path $repoRoot 'artifacts\phase1-wpf-interactive-review\WP14-WPF-INTERACTIVE-REVIEW.json'
if (Test-Path -LiteralPath $interactiveReviewPath) {
    $interactiveReview = Read-JsonRecord 'artifacts\phase1-wpf-interactive-review\WP14-WPF-INTERACTIVE-REVIEW.json'
    Assert-SafeRecord $interactiveReview 'Interactive WPF review'
    if ($interactiveReview.measurements.target_content_read -ne $false -or
        $interactiveReview.measurements.text_reader_invocation_count -ne 0 -or
        $interactiveReview.measurements.repository_invocation_count -ne 0) {
        throw 'Interactive WPF review contains an unexpected content-read measurement.'
    }

    $reviewMeasurements = $interactiveReview.measurements
    if ($reviewMeasurements.probe_started -eq $true -and [string]$reviewMeasurements.probe_outcome -eq 'NotRun') {
        throw 'Interactive WPF review marks a started probe as NotRun.'
    }
    if ($interactiveReview.outcome -eq 'Pass' -and [string]$reviewMeasurements.probe_outcome -ne 'Pass') {
        throw 'Interactive WPF review outcome contradicts its probe outcome.'
    }
    if ($interactiveReview.outcome -eq 'Pass' -and
        ([int]$reviewMeasurements.probe_focused_metadata_count -le 0 -or
         [int]$reviewMeasurements.probe_target_foreground_observation_count -le 0 -or
         [string]$reviewMeasurements.probe_delivery_diagnosis -ne 'FixtureFocused' -or
         [int]$reviewMeasurements.probe_uia_metadata_failure_count -ne 0)) {
        throw 'Interactive WPF Pass lacks focused foreground metadata evidence.'
    }
}

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE1-EVIDENCE-PACKAGE-GUARD'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE1_EVIDENCE_PACKAGE_INTEGRITY'
    outcome = 'Pass'
    durationMs = 0
    failureCodes = @()
    invariantIds = @('P-001', 'P-002', 'P-003', 'P-026', 'P-028', 'P-029', 'P-030', 'C-003', 'C-016', 'C-017', 'C-022', 'C-023', 'C-025')
    testIds = @('PERF-006', 'EXP-001', 'EXP-002')
    measurements = [ordered]@{
        records_checked = $recordPaths.Count
        provider_stress_outcome = [string]$stress.outcome
        provider_stress_requests = [int64]$stress.measurements.reconciliation_request_count
        provider_stress_reads = [int64]$stress.measurements.uia_metadata_read_count
        wpf_repeat_path = $wpf.Path.Substring($repoRoot.Length + 1).Replace('\','/')
        wpf_repeat_count = $repeatCount
        wpf_activation_blocked_attempts = $activationBlocked
        wpf_activation_attempts = $activationAttempts
        wpf_activation_successes = $activationSuccesses
        wpf_focused_metadata_count = $focusedCount
        interactive_review_present = ($null -ne $interactiveReview)
        interactive_review_outcome = if ($null -eq $interactiveReview) { 'NotPresent' } else { [string]$interactiveReview.outcome }
        interactive_review_probe_started = if ($null -eq $interactiveReview) { $false } else { [bool]$interactiveReview.measurements.probe_started }
        interactive_review_preflight_ready = if ($null -eq $interactiveReview) { $false } else { [bool]$interactiveReview.measurements.preflight_ready_for_interactive_fixture }
        text_reader_invocation_count = 0
        repository_invocation_count = 0
    }
    contentSafetyAudit = [ordered]@{
        rawTargetTextCaptured = $false
        rawDynamicUiaStringsLogged = $false
        clipboardRead = $false
        clipboardWritten = $false
        networkTextSent = $false
        testDataClass = 'MetadataOnly'
    }
}

$recordPath = Join-Path $outputPath 'PHASE1-EVIDENCE-PACKAGE-GUARD.json'
$record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 1 evidence package guard: Pass ($recordPath)"
