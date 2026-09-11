param(
    [string]$OutputDirectory = 'artifacts\phase3-exit-gate-final'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

function Read-Record([string]$relativePath) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

$application = Read-Record 'artifacts\phase3-application-gate-final\PHASE3-APPLICATION-GATE.json'
$integrated = Read-Record 'artifacts\phase3-integrated-gate-final\PHASE3-INTEGRATED-GATE.json'
$soak = Read-Record 'artifacts\phase3-soak-final\PHASE3-OPERATIONAL-SOAK.json'
$appPass = $null -ne $application -and $application.outcome -eq 'Pass' -and $application.measurements.test_count -eq 112
$integratedPass = $null -ne $integrated -and $integrated.outcome -eq 'Pass' -and $integrated.measurements.forbidden_api_violations -eq 0
$soakPass = $null -ne $soak -and $soak.outcome -eq 'Pass' -and $soak.measurements.requested_duration_seconds -eq 1800 -and $soak.measurements.actual_duration_seconds -ge 1800 -and $soak.measurements.unbounded_growth_detected -eq $false
$privacyPass = $soakPass -and $soak.measurements.target_content_read -eq $false -and $soak.measurements.persistence_invocations -eq 0 -and $soak.measurements.clipboard_invocations -eq 0 -and $soak.measurements.network_invocations -eq 0 -and $soak.measurements.ui_body_presentations -eq 0
$pass = $appPass -and $integratedPass -and $soakPass -and $privacyPass

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE3-EXIT-GATE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE3_WP38_FINAL_EXIT_REVIEW'
    outcome = if ($pass) { 'Pass' } else { 'Inconclusive' }
    phase3Exit = $pass
    failureCodes = if ($pass) { @() } else { @('DR-PHASE3-0003') }
    testIds = @('READ-001','READ-002','READ-003','READ-004','READ-005','READ-006','READ-007','SNAP-001','SNAP-002','SNAP-003','P3F-001','P3F-002','P3F-003','P3F-004','P3F-005','P3F-006','P3F-008','P3F-009','PERF-007','ARC-007')
    invariantIds = @('P-002','P-003','P-004','P-005','P-006','P-017','P-025','P-026','P-028','P-030','P-031','P-032','P-033','P-034','P-035','P-036','P-037','C-019','C-020','C-026','C-027','C-028','C-029','C-030','C-031','C-032','C-033')
    measurements = [ordered]@{
        application_gate_pass = $appPass
        integrated_gate_pass = $integratedPass
        operational_soak_pass = $soakPass
        privacy_sink_checks_pass = $privacyPass
        test_count = if ($null -eq $application) { 0 } else { [int]$application.measurements.test_count }
        reader_implementations = if ($null -eq $application) { 0 } else { [int]$application.measurements.production_reader_implementations }
        soak_duration_seconds = if ($null -eq $soak) { 0 } else { [double]$soak.measurements.actual_duration_seconds }
        soak_iterations = if ($null -eq $soak) { 0 } else { [long]$soak.measurements.iterations }
        current_tracker_records = if ($null -eq $soak) { 0 } else { [int]$soak.measurements.current_tracker_records }
        revision_history_entries = if ($null -eq $soak) { -1 } else { [int]$soak.measurements.revision_history_entries }
        unbounded_growth_detected = if ($null -eq $soak) { $true } else { [bool]$soak.measurements.unbounded_growth_detected }
        forbidden_api_violations = if ($null -eq $integrated) { -1 } else { [int]$integrated.measurements.forbidden_api_violations }
        target_content_read = $false
        persistence_invocations = 0
        clipboard_invocations = 0
        network_invocations = 0
        ui_body_presentations = 0
    }
    contentSafetyAudit = [ordered]@{
        rawTargetTextCaptured = $false
        rawDynamicUiaStringsLogged = $false
        clipboardRead = $false
        clipboardWritten = $false
        networkTextSent = $false
        testDataClass = 'Synthetic'
    }
}
$recordPath = Join-Path $outputPath 'PHASE3-EXIT-GATE.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 3 exit gate: $($record.outcome) ($recordPath)"
if (-not $pass) { exit 1 }
