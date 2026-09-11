param(
    [string]$OutputDirectory = 'artifacts\phase1-wpf-interactive-review',
    [switch]$PreflightOnly
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

function Read-Record([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Label record was not written: $Path"
    }

    try {
        return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    }
    catch {
        throw "$Label record is not valid JSON."
    }
}

function Assert-SafeRecord([object]$Record, [string]$Label) {
    $audit = $Record.contentSafetyAudit
    if ($Record.schemaVersion -ne 1 -or
        $null -eq $audit -or
        $audit.rawTargetTextCaptured -ne $false -or
        $audit.rawDynamicUiaStringsLogged -ne $false -or
        $audit.clipboardRead -ne $false -or
        $audit.clipboardWritten -ne $false -or
        $audit.networkTextSent -ne $false) {
        throw "$Label content-safety audit failed."
    }
}

$preflightRelative = Join-Path $OutputDirectory 'preflight'
$probeRelative = Join-Path $OutputDirectory 'probe'
$preflightScript = Join-Path $PSScriptRoot 'run_wpf_interactive_preflight.ps1'
$probeScript = Join-Path $PSScriptRoot 'run_wpf_interactive.ps1'

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $preflightScript -OutputDirectory $preflightRelative
if ($LASTEXITCODE -ne 0) {
    throw "Interactive preflight exited with code $LASTEXITCODE."
}

$preflightPath = Join-Path $repoRoot (Join-Path $preflightRelative 'WP14-WPF-INTERACTIVE-PREFLIGHT.json')
$preflight = Read-Record $preflightPath 'Preflight'
Assert-SafeRecord $preflight 'Preflight'
if ($preflight.measurements.target_content_read -ne $false -or
    $preflight.measurements.text_reader_invocation_count -ne 0 -or
    $preflight.measurements.repository_invocation_count -ne 0) {
    throw 'Preflight contains an unexpected content-read measurement.'
}

# The preflight is advisory: a fixture launched by the next step may itself
# become foreground. In normal mode we therefore run the bounded probe and
# preserve both snapshots in one review record. PreflightOnly is a fast,
# content-free CI smoke mode that deliberately does not wait for Console.ReadLine.
$probeExitCode = $null
$probe = $null
$probeMeasurements = $null
if (-not $PreflightOnly) {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $probeScript -OutputDirectory $probeRelative
    $probeExitCode = $LASTEXITCODE

    $probePath = Join-Path $repoRoot (Join-Path $probeRelative 'WP14-WPF-INTERACTIVE.json')
    $probe = Read-Record $probePath 'Interactive probe'
    Assert-SafeRecord $probe 'Interactive probe'
    $probeMeasurements = $probe.measurements
    if ($probeMeasurements.text_reader_invocation_count -ne 0 -or
        $probeMeasurements.window_title_read -ne $false) {
        throw 'Interactive probe contains an unexpected content-read measurement.'
    }
}

$outcome = if ($null -eq $probe) { 'Inconclusive' } elseif ($probe.outcome -eq 'Pass') { 'Pass' } else { 'Inconclusive' }
[string[]]$failureCodes = @()
if ($null -ne $probe -and $outcome -ne 'Pass') {
    $failureCodes = [string[]]@('DR-PHASE1-0003')
}
$review = [ordered]@{
    schemaVersion = 1
    experimentId = 'WP14-WPF-INTERACTIVE-REVIEW'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE1_WPF_INTERACTIVE_REVIEW'
    outcome = $outcome
    durationMs = if ($null -eq $probeMeasurements) { 0 } else { [int64]$probeMeasurements.probe_duration_ms }
    failureCodes = $failureCodes
    invariantIds = @('P-001', 'P-002', 'P-003', 'P-026', 'P-028', 'P-029', 'P-030', 'C-016', 'C-017', 'C-022', 'C-023')
    testIds = @('EXP-001', 'EXP-002')
    measurements = [ordered]@{
        preflight_outcome = [string]$preflight.outcome
        preflight_user_interactive = [bool]$preflight.measurements.user_interactive
        preflight_foreground_window_present = [bool]$preflight.measurements.foreground_window_present
        preflight_input_desktop_accessible = [bool]$preflight.measurements.input_desktop_accessible
        preflight_ready_for_interactive_fixture = [bool]$preflight.measurements.ready_for_interactive_fixture
        probe_started = (-not $PreflightOnly)
        probe_not_run_reason = if ($PreflightOnly) { 'PreflightOnly' } else { $null }
        probe_exit_code = if ($null -eq $probeExitCode) { $null } else { [int]$probeExitCode }
        probe_outcome = if ($null -eq $probe) { 'NotRun' } else { [string]$probe.outcome }
        probe_target_foreground_observation_count = if ($null -eq $probeMeasurements) { 0 } else { [int]$probeMeasurements.target_foreground_observation_count }
        probe_focused_metadata_count = if ($null -eq $probeMeasurements) { 0 } else { [int]$probeMeasurements.focused_metadata_count }
        probe_uia_metadata_read_count = if ($null -eq $probeMeasurements) { 0 } else { [int]$probeMeasurements.uia_metadata_read_count }
        probe_uia_metadata_failure_count = if ($null -eq $probeMeasurements) { 0 } else { [int]$probeMeasurements.uia_metadata_failure_count }
        probe_delivery_diagnosis = if ($null -eq $probeMeasurements) { 'NotRun' } else { [string]$probeMeasurements.delivery_diagnosis }
        probe_preflight_before_foreground_window_present = if ($null -eq $probeMeasurements) { $false } else { [bool]$probeMeasurements.preflight_before_foreground_window_present }
        probe_preflight_after_foreground_window_present = if ($null -eq $probeMeasurements) { $false } else { [bool]$probeMeasurements.preflight_after_foreground_window_present }
        probe_preflight_after_focus_window_present = if ($null -eq $probeMeasurements) { $false } else { [bool]$probeMeasurements.preflight_after_foreground_focus_window_present }
        probe_preflight_after_ready = if ($null -eq $probeMeasurements) { $false } else { [bool]$probeMeasurements.preflight_after_ready_for_interactive_fixture }
        target_content_read = $false
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

$reviewPath = Join-Path $outputPath 'WP14-WPF-INTERACTIVE-REVIEW.json'
$review | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $reviewPath -Encoding utf8
Write-Output "Interactive WPF review: $outcome ($reviewPath)"
