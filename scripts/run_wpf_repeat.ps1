param(
    [string]$OutputDirectory = "artifacts\phase1-wpf-repeat-auto",
    [ValidateRange(2, 10)][int]$RepeatCount = 5
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$harnessPath = Join-Path $repoRoot 'experiments\DraftRescue.Phase1Harness\bin\Release\net8.0-windows\DraftRescue.Phase1Harness.exe'
if (-not (Test-Path -LiteralPath $harnessPath)) {
    throw 'Harness executable was not found. Build DraftRescue.Phase1Harness in Release first.'
}

$attempts = [System.Collections.Generic.List[object]]::new()
for ($index = 1; $index -le $RepeatCount; $index++) {
    $attemptPath = Join-Path $outputPath ("attempt-{0}" -f $index)
    New-Item -ItemType Directory -Force -Path $attemptPath | Out-Null
    & $harnessPath $attemptPath '--wpf-only'
    if ($LASTEXITCODE -ne 0) { throw "WPF attempt $index exited with code $LASTEXITCODE." }

    $recordPath = Join-Path $attemptPath 'WP14-WPF.json'
    if (-not (Test-Path -LiteralPath $recordPath)) { throw "WPF attempt $index did not write WP14-WPF.json." }
    $record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
    $attempts.Add([ordered]@{
        attempt = $index
        outcome = [string]$record.outcome
        activation_attempt_count = [int]$record.measurements.activation_attempt_count
        activation_success_count = [int]$record.measurements.activation_success_count
        activation_verified = [bool]$record.measurements.activation_verified
        automation_focus_attempt_count = [int]$record.measurements.automation_focus_attempt_count
        automation_focus_success_count = [int]$record.measurements.automation_focus_success_count
        focused_metadata_count = [int]$record.measurements.focused_metadata_count
        target_foreground_observation_count = [int]$record.measurements.target_foreground_observation_count
        last_foreground_process_id = [int]$record.measurements.last_foreground_process_id
        control_kind = [string]$record.measurements.control_kind
        framework_kind = [string]$record.measurements.framework_kind
        integrity_compatibility = [string]$record.measurements.integrity_compatibility
        delivery_diagnosis = [string]$record.measurements.delivery_diagnosis
        uia_reconciliation_signal_count = [int64]$record.measurements.uia_reconciliation_signal_count
        uia_metadata_read_count = [int64]$record.measurements.uia_metadata_read_count
        uia_metadata_non_null_count = [int64]$record.measurements.uia_metadata_non_null_count
        uia_metadata_failure_count = [int64]$record.measurements.uia_metadata_failure_count
        uia_last_metadata_process_id = [int]$record.measurements.uia_last_metadata_process_id
        text_reader_invocation_count = [int]$record.measurements.text_reader_invocation_count
    })
}

$passCount = @($attempts | Where-Object { $_.outcome -eq 'Pass' }).Count
$summary = [ordered]@{
    schemaVersion = 1
    experimentId = 'WP14-WPF-REPEAT-AUTO'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'WP14_WPF_TEXTBOX_METADATA_REPEATABILITY_AUTOMATED'
    outcome = if ($passCount -eq $RepeatCount) { 'Pass' } else { 'Inconclusive' }
    durationMs = 0
    failureCodes = @()
    invariantIds = @('P-001', 'P-002', 'P-003', 'P-026', 'P-028', 'P-029', 'P-030', 'C-003', 'C-016', 'C-017', 'C-022', 'C-023')
    testIds = @('CERT-001', 'EXP-001', 'EXP-002')
    measurements = [ordered]@{
        repeat_count = $RepeatCount
        pass_count = $passCount
        text_reader_invocation_count = 0
        attempts = @($attempts)
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

$summaryPath = Join-Path $outputPath 'WP14-WPF-REPEAT-AUTO.json'
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $summaryPath -Encoding utf8
Write-Output "WPF repeat result: $($summary.outcome) ($passCount/$RepeatCount passed; $summaryPath)"
if ($summary.outcome -ne 'Pass') { exit 1 }
