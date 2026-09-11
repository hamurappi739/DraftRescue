param(
    [string]$OutputDirectory = "artifacts\phase1-gate"
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

function Get-IntOrDefault([object]$Value) {
    $parsed = 0
    if ($null -ne $Value -and [int]::TryParse([string]$Value, [ref]$parsed)) { return $parsed }
    return 0
}

$requiredRecords = @(
    'artifacts\phase1\WP14-SYNTH.json',
    'artifacts\phase1\WP14-FAULT.json',
    'artifacts\phase1\WP14-WPF.json',
    'artifacts\phase1\WP14-WPF-REPEAT.json',
    'artifacts\phase1-desktop-ui\WP14-UI.json',
    'artifacts\phase1-hang-evidence\WP14-HANG.json',
    'artifacts\phase1-long\WP14-SOAK.json'
)

$records = @{}
$missing = [System.Collections.Generic.List[string]]::new()
$safetyFailures = [System.Collections.Generic.List[string]]::new()
foreach ($relativePath in $requiredRecords) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $missing.Add($relativePath)
        continue
    }

    try {
        $record = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    }
    catch {
        # A malformed evidence record is treated as missing/unsafe rather than
        # allowing a partially parsed object to influence the gate.
        $missing.Add($relativePath)
        $safetyFailures.Add($relativePath)
        continue
    }
    $records[$relativePath] = $record
    $audit = $record.contentSafetyAudit
    if ($record.schemaVersion -ne 1 -or
        $audit.rawTargetTextCaptured -ne $false -or
        $audit.rawDynamicUiaStringsLogged -ne $false -or
        $audit.clipboardRead -ne $false -or
        $audit.clipboardWritten -ne $false -or
        $audit.networkTextSent -ne $false) {
        $safetyFailures.Add($relativePath)
    }
}

# Automated WPF runs are written to versioned evidence directories while the
# fixture is being certified. Select the newest valid summary by its runId,
# with file timestamp as a deterministic fallback. This prevents the gate from
# silently pinning an older attempt when a newer run has already been produced.
$wpfCandidates = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'artifacts') -Directory -Filter 'phase1-wpf-repeat-*' |
    ForEach-Object {
        $candidatePath = Join-Path $_.FullName 'WP14-WPF-REPEAT-AUTO.json'
        if (-not (Test-Path -LiteralPath $candidatePath)) { return }
        try {
            $candidate = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json
            if ($candidate.schemaVersion -ne 1 -or
                $candidate.experimentId -ne 'WP14-WPF-REPEAT-AUTO' -or
                $candidate.scenarioId -ne 'WP14_WPF_TEXTBOX_METADATA_REPEATABILITY_AUTOMATED' -or
                $null -eq $candidate.measurements) { return }
            $runSort = 0L
            if ($candidate.runId -is [string]) {
                [Int64]::TryParse(($candidate.runId -replace '[^0-9]', ''), [Globalization.NumberStyles]::Integer, [Globalization.CultureInfo]::InvariantCulture, [ref]$runSort) | Out-Null
            }
            [pscustomobject]@{ Path = $candidatePath; Record = $candidate; RunSort = $runSort; LastWriteUtc = (Get-Item -LiteralPath $candidatePath).LastWriteTimeUtc }
        }
        catch {
            # Ignore malformed candidates here; the diagnostics below expose
            # that no valid automated summary was available.
        }
    } |
    Sort-Object -Property RunSort, LastWriteUtc -Descending
$wpfSelected = $wpfCandidates | Select-Object -First 1
$wpfRepeatPath = if ($null -eq $wpfSelected) { $null } else { [string]$wpfSelected.Path }
$wpfRepeat = if ($null -eq $wpfSelected) { $null } else { $wpfSelected.Record }
$wpfRepeatAudit = if ($null -eq $wpfRepeat) { $null } else { $wpfRepeat.contentSafetyAudit }
$wpfRepeatSafetyPass = ($null -ne $wpfRepeatAudit -and
    $wpfRepeatAudit.rawTargetTextCaptured -eq $false -and
    $wpfRepeatAudit.rawDynamicUiaStringsLogged -eq $false -and
    $wpfRepeatAudit.clipboardRead -eq $false -and
    $wpfRepeatAudit.clipboardWritten -eq $false -and
    $wpfRepeatAudit.networkTextSent -eq $false)
$wpfRepeatAttempts = if ($null -eq $wpfRepeat) { @() } else { @($wpfRepeat.measurements.attempts) }
$wpfActivationBlockedCount = @($wpfRepeatAttempts | Where-Object { [string]$_.delivery_diagnosis -eq 'ActivationBlocked' }).Count
$wpfActivationAttemptCount = [int64](@($wpfRepeatAttempts | ForEach-Object { Get-IntOrDefault $_.activation_attempt_count } | Measure-Object -Sum).Sum)
$wpfActivationSuccessCount = [int64](@($wpfRepeatAttempts | ForEach-Object { Get-IntOrDefault $_.activation_success_count } | Measure-Object -Sum).Sum)
$wpfAutomationFocusAttemptCount = [int64](@($wpfRepeatAttempts | ForEach-Object { Get-IntOrDefault $_.automation_focus_attempt_count } | Measure-Object -Sum).Sum)
$wpfAutomationFocusSuccessCount = [int64](@($wpfRepeatAttempts | ForEach-Object { Get-IntOrDefault $_.automation_focus_success_count } | Measure-Object -Sum).Sum)
$interactiveReviewPath = Join-Path $repoRoot 'artifacts\phase1-wpf-interactive-review\WP14-WPF-INTERACTIVE-REVIEW.json'
$interactiveReview = $null
$interactiveReviewParseFailed = $false
if (Test-Path -LiteralPath $interactiveReviewPath) {
    try {
        $interactiveReview = Get-Content -LiteralPath $interactiveReviewPath -Raw | ConvertFrom-Json
    }
    catch {
        $interactiveReviewParseFailed = $true
    }
}
$interactiveReviewAudit = if ($null -eq $interactiveReview) { $null } else { $interactiveReview.contentSafetyAudit }
$interactiveReviewSafetyPass = ($null -ne $interactiveReviewAudit -and
    -not $interactiveReviewParseFailed -and
    $interactiveReview.schemaVersion -eq 1 -and
    $interactiveReviewAudit.rawTargetTextCaptured -eq $false -and
    $interactiveReviewAudit.rawDynamicUiaStringsLogged -eq $false -and
    $interactiveReviewAudit.clipboardRead -eq $false -and
    $interactiveReviewAudit.clipboardWritten -eq $false -and
    $interactiveReviewAudit.networkTextSent -eq $false)
$interactiveReviewMeasurements = if ($null -eq $interactiveReview) { $null } else { $interactiveReview.measurements }
$interactiveWpfConfirmationPass = ($null -ne $interactiveReviewMeasurements -and
    $interactiveReviewSafetyPass -and
    $interactiveReview.outcome -eq 'Pass' -and
    $interactiveReviewMeasurements.probe_started -eq $true -and
    (Get-IntOrDefault $interactiveReviewMeasurements.probe_exit_code) -eq 0 -and
    $interactiveReviewMeasurements.probe_outcome -eq 'Pass' -and
    $interactiveReviewMeasurements.probe_delivery_diagnosis -eq 'FixtureFocused' -and
    (Get-IntOrDefault $interactiveReviewMeasurements.probe_target_foreground_observation_count) -gt 0 -and
    (Get-IntOrDefault $interactiveReviewMeasurements.probe_focused_metadata_count) -gt 0 -and
    (Get-IntOrDefault $interactiveReviewMeasurements.probe_uia_metadata_failure_count) -eq 0 -and
    $interactiveReviewMeasurements.target_content_read -eq $false -and
    (Get-IntOrDefault $interactiveReviewMeasurements.text_reader_invocation_count) -eq 0 -and
    (Get-IntOrDefault $interactiveReviewMeasurements.repository_invocation_count) -eq 0)
$soak = $records['artifacts\phase1-long\WP14-SOAK.json']
$hang = $records['artifacts\phase1-hang-evidence\WP14-HANG.json']
$ui = $records['artifacts\phase1-desktop-ui\WP14-UI.json']

$criteria = [ordered]@{
    required_records_present = ($missing.Count -eq 0)
    content_safety_audits_pass = ($safetyFailures.Count -eq 0)
    synthetic_coalescing_pass = ($records['artifacts\phase1\WP14-SYNTH.json'].outcome -eq 'Pass')
    provider_fault_pass = ($records['artifacts\phase1\WP14-FAULT.json'].outcome -eq 'Pass')
    bounded_hung_shutdown_pass = ($null -ne $hang -and $hang.outcome -eq 'Pass' -and $hang.measurements.ui_thread_blocked -eq $false)
    desktop_shell_responsiveness_pass = ($null -ne $ui -and $ui.outcome -eq 'Pass' -and $ui.measurements.non_responding_samples -eq 0)
    formal_long_soak_pass = ($null -ne $soak -and $soak.outcome -eq 'Pass' -and $soak.measurements.soak_duration_seconds -eq 1800)
    wpf_focus_delivery_pass = (($null -ne $wpfRepeat -and
        $wpfRepeatSafetyPass -and
        $wpfRepeat.outcome -eq 'Pass' -and
        (Get-IntOrDefault $wpfRepeat.measurements.repeat_count) -gt 0 -and
        (Get-IntOrDefault $wpfRepeat.measurements.pass_count) -eq (Get-IntOrDefault $wpfRepeat.measurements.repeat_count) -and
        @($wpfRepeat.measurements.attempts).Count -eq (Get-IntOrDefault $wpfRepeat.measurements.repeat_count) -and
        $wpfActivationBlockedCount -eq 0) -or $interactiveWpfConfirmationPass)
}

$failureCodes = [System.Collections.Generic.List[string]]::new()
if ($missing.Count -gt 0) { $failureCodes.Add('DR-PHASE1-0001') }
if ($safetyFailures.Count -gt 0) { $failureCodes.Add('DR-PHASE1-0002') }
if ($null -ne $wpfRepeat -and -not $wpfRepeatSafetyPass -and -not $failureCodes.Contains('DR-PHASE1-0002')) { $failureCodes.Add('DR-PHASE1-0002') }
if (-not $criteria.wpf_focus_delivery_pass) { $failureCodes.Add('DR-PHASE1-0003') }
if (-not $criteria.formal_long_soak_pass) { $failureCodes.Add('DR-PHASE1-0004') }

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE1-EXIT-GATE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE1_EVIDENCE_REVIEW'
    outcome = if ($failureCodes.Count -eq 0 -and @($criteria.Values | Where-Object { $_ -eq $false }).Count -eq 0) { 'Pass' } else { 'Inconclusive' }
    durationMs = 0
    failureCodes = @($failureCodes)
    invariantIds = @('P-001', 'P-002', 'P-003', 'P-025', 'P-026', 'P-028', 'P-029', 'P-030', 'C-003', 'C-016', 'C-017', 'C-018', 'C-022', 'C-023', 'C-025')
    testIds = @('OBS-005', 'OBS-006', 'OBS-007', 'FI-001', 'FI-002', 'FI-004', 'PERF-005', 'PERF-006', 'PERF-007', 'CERT-001', 'EXP-001', 'EXP-002')
    criteria = $criteria
    diagnostics = [ordered]@{
        missing_records = @($missing)
        safety_failures = @($safetyFailures)
        wpf_repeat_selected_path = if ($null -eq $wpfRepeatPath) { $null } else { $wpfRepeatPath.Substring($repoRoot.Length + 1).Replace('\','/') }
        wpf_repeat_candidate_count = @($wpfCandidates).Count
        wpf_repeat_safety_audit_pass = $wpfRepeatSafetyPass
        wpf_repeat_outcome = if ($null -eq $wpfRepeat) { 'Missing' } else { [string]$wpfRepeat.outcome }
        wpf_repeat_pass_count = if ($null -eq $wpfRepeat) { 0 } else { Get-IntOrDefault $wpfRepeat.measurements.pass_count }
        wpf_repeat_count = if ($null -eq $wpfRepeat) { 0 } else { Get-IntOrDefault $wpfRepeat.measurements.repeat_count }
        automated_wpf_repeatability_pass = ($null -ne $wpfRepeat -and
            $wpfRepeatSafetyPass -and
            $wpfRepeat.outcome -eq 'Pass' -and
            (Get-IntOrDefault $wpfRepeat.measurements.repeat_count) -gt 0 -and
            (Get-IntOrDefault $wpfRepeat.measurements.pass_count) -eq (Get-IntOrDefault $wpfRepeat.measurements.repeat_count) -and
            @($wpfRepeat.measurements.attempts).Count -eq (Get-IntOrDefault $wpfRepeat.measurements.repeat_count) -and
            $wpfActivationBlockedCount -eq 0)
        interactive_review_present = ($null -ne $interactiveReview)
        interactive_review_parse_failed = $interactiveReviewParseFailed
        interactive_review_safety_audit_pass = $interactiveReviewSafetyPass
        interactive_wpf_confirmation_pass = $interactiveWpfConfirmationPass
        interactive_review_path = if (Test-Path -LiteralPath $interactiveReviewPath) { $interactiveReviewPath.Substring($repoRoot.Length + 1).Replace('\','/') } else { $null }
        wpf_evidence_mode = if ($interactiveWpfConfirmationPass) { 'InteractiveConfirmation' } elseif ($null -ne $wpfRepeat) { 'AutomatedRepeat' } else { 'Missing' }
        wpf_activation_blocked_attempt_count = $wpfActivationBlockedCount
        wpf_activation_attempt_count = $wpfActivationAttemptCount
        wpf_activation_success_count = $wpfActivationSuccessCount
        wpf_automation_focus_attempt_count = $wpfAutomationFocusAttemptCount
        wpf_automation_focus_success_count = $wpfAutomationFocusSuccessCount
        long_soak_duration_seconds = if ($null -eq $soak) { 0 } else { Get-IntOrDefault $soak.measurements.soak_duration_seconds }
        hang_dispose_duration_ms = if ($null -eq $hang) { 0 } else { Get-IntOrDefault $hang.measurements.dispose_duration_ms }
        desktop_non_responding_samples = if ($null -eq $ui) { -1 } else { Get-IntOrDefault $ui.measurements.non_responding_samples }
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

$recordPath = Join-Path $outputPath 'PHASE1-EXIT-GATE.json'
$record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 1 exit gate: $($record.outcome) ($recordPath)"
if ($record.outcome -ne 'Pass') { exit 1 }
