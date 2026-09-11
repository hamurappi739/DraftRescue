param(
    [string]$OutputDirectory = 'artifacts\phase1-harness-process-guard',
    [ValidateRange(1, 30)][int]$SoakSeconds = 1
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
$runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
$runRelative = Join-Path $OutputDirectory ("run-" + $runId)
$runPath = Join-Path $repoRoot $runRelative
New-Item -ItemType Directory -Force -Path $runPath | Out-Null

$processNames = @('dotnet', 'DraftRescue.Phase1Harness', 'DraftRescue.Tests')
$baselineProcessIds = @(
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in $processNames } |
        Select-Object -ExpandProperty Id
)
$startedAt = [DateTime]::UtcNow
$cleanedProcessCount = 0

function Get-NewHarnessProcesses {
    @(
        Get-Process -ErrorAction SilentlyContinue |
            Where-Object {
                $_.Name -in $processNames -and
                $_.Id -notin $baselineProcessIds -and
                $_.StartTime.ToUniversalTime() -ge $startedAt.AddSeconds(-2)
            }
    )
}

function Cleanup-NewHarnessProcesses {
    foreach ($process in @(Get-NewHarnessProcesses)) {
        try {
            Stop-Process -Id $process.Id -Force -ErrorAction Stop
            $script:cleanedProcessCount++
        }
        catch {
            # Cleanup is best-effort and never records process/exception text.
        }
    }
}

try {
    & (Join-Path $PSScriptRoot 'run_phase1_wp14.ps1') -OutputDirectory $runRelative -SoakSeconds $SoakSeconds
    if ($LASTEXITCODE -ne 0) {
        throw "WP-1.4 process guard harness exited with code $LASTEXITCODE."
    }
}
finally {
    Cleanup-NewHarnessProcesses
}

$requiredNames = @('WP14-SYNTH.json', 'WP14-FAULT.json', 'WP14-HANG.json', 'WP14-RESP.json', 'WP14-SOAK.json', 'WP14-WPF.json')
$records = @(Get-ChildItem -LiteralPath $runPath -Filter 'WP14-*.json' -File)
foreach ($requiredName in $requiredNames) {
    if (-not (Test-Path -LiteralPath (Join-Path $runPath $requiredName))) {
        throw "Missing process-guard evidence record: $requiredName"
    }
}

foreach ($recordFile in $records) {
    try {
        $record = Get-Content -LiteralPath $recordFile.FullName -Raw | ConvertFrom-Json
    }
    catch {
        throw "Malformed process-guard evidence record: $($recordFile.Name)"
    }

    $audit = $record.contentSafetyAudit
    if ($record.schemaVersion -ne 1 -or
        $null -eq $audit -or
        $audit.rawTargetTextCaptured -ne $false -or
        $audit.rawDynamicUiaStringsLogged -ne $false -or
        $audit.clipboardRead -ne $false -or
        $audit.clipboardWritten -ne $false -or
        $audit.networkTextSent -ne $false) {
        throw "Content-safety audit failed for $($recordFile.Name)."
    }
}

$leakedProcesses = @(Get-NewHarnessProcesses)
if ($leakedProcesses.Count -ne 0) {
    throw 'Harness process guard detected a process that survived cleanup.'
}

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE1-HARNESS-PROCESS-GUARD'
    runId = $runId
    scenarioId = 'PHASE1_DIRECT_HARNESS_PROCESS_LIFECYCLE'
    outcome = 'Pass'
    durationMs = 0
    failureCodes = @()
    invariantIds = @('P-001', 'P-002', 'P-003', 'P-026', 'P-028', 'P-029', 'P-030', 'C-016', 'C-017', 'C-022', 'C-023', 'C-025')
    testIds = @('FI-001', 'FI-002', 'FI-004', 'PERF-005', 'EXP-001', 'EXP-002')
    measurements = [ordered]@{
        launcher = 'DirectReleaseExe'
        soak_seconds = $SoakSeconds
        records_checked = $records.Count
        required_records = $requiredNames.Count
        cleaned_process_count = $cleanedProcessCount
        leaked_process_count = 0
        text_reader_invocation_count = 0
        repository_invocation_count = 0
        target_content_read = $false
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

$recordPath = Join-Path $outputPath 'WP14-HARNESS-PROCESS-GUARD.json'
$record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 1 harness process guard: Pass ($recordPath)"
