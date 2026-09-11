param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-target-certification',
    [string]$DiskFullEvidencePath = ''
)

$ErrorActionPreference = 'Stop'
$runnerStart = Get-Date
$baselineDotnetIds = @(Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object { [int]$_.Id })
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

function Invoke-Probe([string]$scriptName, [string]$probeOutputDirectory) {
    $probeOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $scriptName) `
        -RepositoryRoot $RepositoryRoot -OutputDirectory $probeOutputDirectory 2>&1)
    $probeOutput | ForEach-Object { Write-Host $_ }
    return [int]$LASTEXITCODE
}

$environmentExit = Invoke-Probe 'probe_phase4_target_environment.ps1' $OutputDirectory
$dpapiExit = Invoke-Probe 'probe_phase4_dpapi_runtime.ps1' $OutputDirectory
$killExit = Invoke-Probe 'run_phase4_process_kill_probe.ps1' $OutputDirectory

$environmentPath = Join-Path $outputPath 'PHASE4-TARGET-ENVIRONMENT-PROBE.json'
$dpapiPath = Join-Path $outputPath 'PHASE4-DPAPI-RUNTIME-PROBE.json'
$killPath = Join-Path $outputPath 'PHASE4-PROCESS-KILL-PROBE.json'
$environment = if (Test-Path -LiteralPath $environmentPath) { Get-Content -LiteralPath $environmentPath -Raw | ConvertFrom-Json } else { $null }
$dpapi = if (Test-Path -LiteralPath $dpapiPath) { Get-Content -LiteralPath $dpapiPath -Raw | ConvertFrom-Json } else { $null }
$kill = if (Test-Path -LiteralPath $killPath) { Get-Content -LiteralPath $killPath -Raw | ConvertFrom-Json } else { $null }

$diskFull = $null
$diskFullLoadFailure = $null
if (-not [string]::IsNullOrWhiteSpace($DiskFullEvidencePath)) {
    $diskFullFull = [System.IO.Path]::GetFullPath($DiskFullEvidencePath)
    if (-not (Test-Path -LiteralPath $diskFullFull -PathType Leaf)) {
        $diskFullLoadFailure = 'DiskFullEvidenceNotFound'
    }
    else {
        try { $diskFull = Get-Content -LiteralPath $diskFullFull -Raw | ConvertFrom-Json }
        catch { $diskFullLoadFailure = 'DiskFullEvidenceMalformed' }
    }
}

$profileLoaded = $null -ne $environment -and $null -ne $environment.profile -and [bool]$environment.profile.loaded
$dpapiPass = $null -ne $dpapi -and [bool]$dpapi.available -and [string]$dpapi.failureCode -eq 'None'
$killPass = $null -ne $kill -and [string]$kill.outcome -eq 'Pass' -and [bool]$kill.rollbackPreservedOldRow
$diskFullHasTypedPrivacy = $null -ne $diskFull -and $null -ne $diskFull.PSObject.Properties['syntheticOnly'] -and $null -ne $diskFull.PSObject.Properties['containsSecrets']
$diskFullPass = $diskFullHasTypedPrivacy -and [string]$diskFull.classification -eq 'RealControlledDiskFull' -and [string]$diskFull.outcome -eq 'Pass' -and -not [bool]$diskFull.syntheticOnly -and -not [bool]$diskFull.containsSecrets

$checks = @(
    [ordered]@{ id = 'environment.profileLoaded'; status = $(if ($profileLoaded) { 'Pass' } else { 'PendingTargetCertification' }); evidence = 'PHASE4-TARGET-ENVIRONMENT-PROBE.json' },
    [ordered]@{ id = 'dpapi.currentUser.roundtrip'; status = $(if ($dpapiPass) { 'Pass' } elseif ($null -ne $dpapi -and [string]$dpapi.failureCode -eq 'DpapiFailure') { 'PendingTargetCertification' } else { 'Inconclusive' }); evidence = 'PHASE4-DPAPI-RUNTIME-PROBE.json' },
    [ordered]@{ id = 'processKill.rollback'; status = $(if ($killPass) { 'Pass' } else { 'Fail' }); evidence = 'PHASE4-PROCESS-KILL-PROBE.json' },
    [ordered]@{ id = 'diskfull.controlled.real'; status = $(if ($diskFullPass) { 'Pass' } elseif ($null -ne $diskFullLoadFailure) { 'Inconclusive' } elseif ($null -eq $diskFull) { 'PendingTargetCertification' } else { 'Fail' }); evidence = $(if ($null -ne $diskFull) { 'PHASE4-DISK-FULL-PROBE.json' } else { $null }) }
)
$pending = @($checks | Where-Object { $_.status -ne 'Pass' } | ForEach-Object { $_.id })
$hasFail = @($checks | Where-Object { $_.status -eq 'Fail' }).Count -gt 0
$outcome = if ($hasFail) { 'Fail' } elseif ($pending.Count -eq 0) { 'Pass' } else { 'Inconclusive' }
$cleanedDotnetProcessIds = [System.Collections.Generic.List[int]]::new()
$leakedDotnetProcessIds = [System.Collections.Generic.List[int]]::new()
foreach ($process in @(Get-Process dotnet -ErrorAction SilentlyContinue)) {
    $isNew = $baselineDotnetIds -notcontains [int]$process.Id
    $startedDuringRun = $false
    try { $startedDuringRun = $process.StartTime -ge $runnerStart } catch { }
    if (-not ($isNew -and $startedDuringRun -and [string]::IsNullOrEmpty($process.MainWindowTitle))) { continue }
    try {
        Stop-Process -Id $process.Id -Force -ErrorAction Stop
        $cleanedDotnetProcessIds.Add([int]$process.Id)
    }
    catch { $leakedDotnetProcessIds.Add([int]$process.Id) }
}
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-TARGET-CERTIFICATION'
    outcome = $outcome
    phase4ExitReady = ($outcome -eq 'Pass')
    environmentProbeExitCode = $environmentExit
    dpapiProbeHarnessExitCode = $dpapiExit
    dpapiProbeResultExitCode = $(if ($null -ne $dpapi -and $null -ne $dpapi.probeExitCode) { [int]$dpapi.probeExitCode } else { $null })
    diskFullEvidenceLoadStatus = $(if ($null -ne $diskFullLoadFailure) { $diskFullLoadFailure } elseif ($null -ne $diskFull) { 'Loaded' } else { 'NotSupplied' })
    environmentProfileLoadState = $(if ($null -ne $environment -and $null -ne $environment.profile) { [string]$environment.profile.loadState } else { $null })
    environmentProfileLoadStateQueryAvailable = $(if ($null -ne $environment -and $null -ne $environment.profile) { [bool]$environment.profile.loadStateQueryAvailable } else { $false })
    processKillProbeExitCode = $killExit
    checks = $checks
    pendingItems = $pending
    nextAction = $(if ($outcome -eq 'Pass') { 'Rerun scripts/run_phase4_exit_gate.ps1 on this certified target' } else { 'Use a loaded user profile and a disposable controlled quota/virtual disk, then rerun this script' })
    ownedDotnetProcessCleanup = [ordered]@{
        baselineCount = $baselineDotnetIds.Count
        cleanedCount = $cleanedDotnetProcessIds.Count
        leakedCount = $leakedDotnetProcessIds.Count
        leakedProcessIds = @($leakedDotnetProcessIds)
    }
    containsSecrets = $false
    syntheticEvidenceOnly = -not $diskFullPass
}
$recordPath = Join-Path $outputPath 'PHASE4-TARGET-CERTIFICATION.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 target certification: $($record.outcome) ($recordPath)"
if ($record.outcome -eq 'Fail') { exit 2 }
if ($record.outcome -ne 'Pass') { exit 1 }
