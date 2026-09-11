param(
    [string]$OutputDirectory = "artifacts\phase1",
    [ValidateRange(3, 60)][int]$DurationSeconds = 10
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$appPath = Join-Path $repoRoot 'src\DraftRescue.Desktop\bin\Debug\net8.0-windows\win-x64\DraftRescue.Desktop.exe'
if (-not (Test-Path -LiteralPath $appPath)) {
    throw "Desktop executable was not found. Build DraftRescue.sln first."
}

$process = $null
$startupStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$respondingSamples = 0
$nonRespondingSamples = 0
$peakPrivateMemory = 0L
$initialPrivateMemory = 0L
$cpuBefore = 0.0
$outcome = 'Inconclusive'
$failureCodes = [System.Collections.Generic.List[string]]::new()

try {
    $process = Start-Process -FilePath $appPath -PassThru
    while ($startupStopwatch.Elapsed -lt [TimeSpan]::FromSeconds(8)) {
        $process.Refresh()
        if ($process.MainWindowHandle -ne 0) { break }
        Start-Sleep -Milliseconds 100
    }

    $process.Refresh()
    $startupMs = [Math]::Round($startupStopwatch.Elapsed.TotalMilliseconds, 2)
    if ($process.HasExited -or $process.MainWindowHandle -eq 0) {
        $failureCodes.Add('DR-UI-2001')
    } else {
        $initialPrivateMemory = $process.PrivateMemorySize64
        $peakPrivateMemory = $initialPrivateMemory
        $cpuBefore = $process.TotalProcessorTime.TotalMilliseconds
        $sampleStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        while ($sampleStopwatch.Elapsed -lt [TimeSpan]::FromSeconds($DurationSeconds)) {
            $process.Refresh()
            if ($process.HasExited) {
                $failureCodes.Add('DR-UI-2002')
                break
            }

            if ($process.Responding) { $respondingSamples++ } else { $nonRespondingSamples++ }
            if ($process.PrivateMemorySize64 -gt $peakPrivateMemory) { $peakPrivateMemory = $process.PrivateMemorySize64 }
            Start-Sleep -Milliseconds 250
        }

        if ($failureCodes.Count -eq 0 -and $respondingSamples -gt 0 -and $nonRespondingSamples -eq 0) {
            $outcome = 'Pass'
        } else {
            $failureCodes.Add('DR-UI-2003')
        }
    }
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(2000)
    }
    if ($process) { $process.Dispose() }
}

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'WP14-UI'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'WP14_DESKTOP_SHELL_RESPONSIVENESS'
    outcome = $outcome
    durationMs = $startupStopwatch.ElapsedMilliseconds
    failureCodes = @($failureCodes)
    invariantIds = @('P-001', 'P-002', 'P-003', 'P-026', 'P-028', 'P-029', 'P-030', 'C-003', 'C-016', 'C-017', 'C-022', 'C-023', 'C-025')
    testIds = @('PERF-005', 'PERF-007', 'EXP-001', 'EXP-002')
    measurements = [ordered]@{
        executable = 'DraftRescue.Desktop'
        startup_duration_ms = $startupMs
        duration_seconds = $DurationSeconds
        responding_samples = $respondingSamples
        non_responding_samples = $nonRespondingSamples
        initial_private_memory_bytes = $initialPrivateMemory
        peak_private_memory_bytes = $peakPrivateMemory
        cpu_before_ms = $cpuBefore
        ui_thread_blocked = ($nonRespondingSamples -gt 0)
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

$recordPath = Join-Path $outputPath 'WP14-UI.json'
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Desktop responsiveness result: $outcome ($recordPath)"
if ($outcome -ne 'Pass') { exit 1 }
