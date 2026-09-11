param(
    [string]$OutputDirectory = 'artifacts\phase4-exit-gate',
    [switch]$SkipBuild,
    [string]$DiskFullEvidencePath = ''
)

$ErrorActionPreference = 'Stop'
$runnerStart = Get-Date
$baselineDotnetIds = @(Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object { [int]$_.Id })
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$buildExit = 0
if (-not $SkipBuild) {
    # Avalonia's reference-assembly target can leave a stale generated DLL
    # locked between runs. Remove only that exact intermediate under the
    # Desktop project's obj tree, then recreate its parent directory so the
    # verification build is deterministic without touching source artifacts.
    $desktopObjRoot = Join-Path $repoRoot 'src\DraftRescue.Desktop\obj'
    $desktopRefDir = Join-Path $desktopObjRoot 'Debug\net8.0-windows\win-x64\ref'
    $desktopRefDll = Join-Path $desktopRefDir 'DraftRescue.Desktop.dll'
    $desktopRefFull = [System.IO.Path]::GetFullPath($desktopRefDll)
    $desktopObjFull = [System.IO.Path]::GetFullPath($desktopObjRoot)
    if (-not $desktopRefFull.StartsWith($desktopObjFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Unsafe generated reference path'
    }
    if (Test-Path -LiteralPath $desktopRefDll) { Remove-Item -LiteralPath $desktopRefDll -Force }
    New-Item -ItemType Directory -Force -Path $desktopRefDir | Out-Null
    & dotnet build (Join-Path $repoRoot 'DraftRescue.sln') --no-restore -c Debug --nologo
    $buildExit = $LASTEXITCODE
}
$testOutput = @(& dotnet test (Join-Path $repoRoot 'tests\DraftRescue.Tests\DraftRescue.Tests.csproj') --no-build -c Debug --nologo 2>&1)
$testOutput | ForEach-Object { Write-Output $_ }
$testExit = $LASTEXITCODE
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'probe_phase4_dpapi_runtime.ps1') -RepositoryRoot $repoRoot -OutputDirectory $OutputDirectory
$probeExit = $LASTEXITCODE
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'run_phase4_process_kill_probe.ps1') -RepositoryRoot $repoRoot -OutputDirectory $OutputDirectory
$killProbeExit = $LASTEXITCODE
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'probe_phase4_target_environment.ps1') -RepositoryRoot $repoRoot -OutputDirectory $OutputDirectory
$environmentProbeExit = $LASTEXITCODE
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'phase4_artifact_privacy_guard.ps1') -RepositoryRoot $repoRoot -OutputDirectory 'artifacts\phase4-artifact-privacy-guard'
$artifactPrivacyGuardExit = $LASTEXITCODE

$requiredArtifacts = @(
    'artifacts\phase4-contracts-gate\PHASE4-CONTRACTS-GATE.json',
    'artifacts\phase4-dpapi-gate\PHASE4-DPAPI-GATE.json',
    'artifacts\phase4-sqlite-gate\PHASE4-SQLITE-GATE.json',
    'artifacts\phase4-coordinator-gate\PHASE4-COORDINATOR-GATE.json',
    'artifacts\phase4-recovery-gate\PHASE4-RECOVERY-GATE.json',
    'artifacts\phase4-fault-gate\PHASE4-FAULT-GATE.json'
)
$artifactRecords = @()
$missingArtifacts = [System.Collections.Generic.List[string]]::new()
foreach ($relative in $requiredArtifacts) {
    $path = Join-Path $repoRoot $relative
    if (-not (Test-Path -LiteralPath $path)) { $missingArtifacts.Add($relative); continue }
    $artifactRecords += [pscustomobject]@{ path = $relative; record = (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json) }
}
$failedArtifacts = @($artifactRecords | Where-Object { $_.record.outcome -ne 'Pass' } | ForEach-Object { $_.path })

$testTotal = $null
$testPassed = $null
$testText = $testOutput -join "`n"
$summaryMatch = [regex]::Match($testText, '(?m)(\d+)\D+(\d+)\D+(\d+)\D+(\d+)\D+\d+s[^\r\n]*DraftRescue\.Tests\.dll')
if ($summaryMatch.Success) {
    $testPassed = [int]$summaryMatch.Groups[2].Value
    $testTotal = [int]$summaryMatch.Groups[4].Value
}
$totalMatch = [regex]::Match($testText, '(?i)(?:всего|total tests?)\s*:\s*(\d+)')
$passedMatches = [regex]::Matches($testText, '(?i)(?:пройдено|passed)\s*:\s*(\d+)')
if ($null -eq $testTotal -and $totalMatch.Success) { $testTotal = [int]$totalMatch.Groups[1].Value }
if ($null -eq $testPassed -and $passedMatches.Count -gt 0) { $testPassed = [int]$passedMatches[$passedMatches.Count - 1].Groups[1].Value }
$testSuite = if ($null -ne $testTotal -and $null -ne $testPassed) { "$testPassed/$testTotal" } else { 'unknown' }

$phase4Source = @(
    (Join-Path $repoRoot 'src\DraftRescue.Application\Contracts\Persistence'),
    (Join-Path $repoRoot 'src\DraftRescue.Application\Persistence'),
    (Join-Path $repoRoot 'src\DraftRescue.Infrastructure\Persistence'),
    (Join-Path $repoRoot 'src\DraftRescue.Platform.Windows\Security')
)
$sourceFiles = @($phase4Source | ForEach-Object { if (Test-Path -LiteralPath $_) { Get-ChildItem -LiteralPath $_ -Recurse -File -Filter '*.cs' } })
$sourceText = ($sourceFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
$sourceViolations = [System.Collections.Generic.List[string]]::new()
foreach ($token in @('journal_mode=WAL', 'GetAllDecrypted', 'File.WriteAllText', 'File.AppendAllText', 'HttpClient', 'DROP TABLE', 'Preview(', 'Copy(', 'Restore(')) {
    if ($sourceText -match [regex]::Escape($token)) { $sourceViolations.Add($token) }
}

$dpapiProbe = $false
$probePath = Join-Path $outputPath 'PHASE4-DPAPI-RUNTIME-PROBE.json'
$probe = if (Test-Path -LiteralPath $probePath) { Get-Content -LiteralPath $probePath -Raw | ConvertFrom-Json } else { $null }
if ($null -ne $probe) { $dpapiProbe = [bool]$probe.available }
$killProbePath = Join-Path $outputPath 'PHASE4-PROCESS-KILL-PROBE.json'
$killProbe = if (Test-Path -LiteralPath $killProbePath) { Get-Content -LiteralPath $killProbePath -Raw | ConvertFrom-Json } else { $null }
$killRollback = $null -ne $killProbe -and [bool]$killProbe.rollbackPreservedOldRow
$environmentProbePath = Join-Path $outputPath 'PHASE4-TARGET-ENVIRONMENT-PROBE.json'
$environmentProbe = if (Test-Path -LiteralPath $environmentProbePath) { Get-Content -LiteralPath $environmentProbePath -Raw | ConvertFrom-Json } else { $null }
$artifactPrivacyGuardPath = Join-Path $repoRoot 'artifacts\phase4-artifact-privacy-guard\PHASE4-ARTIFACT-PRIVACY-GUARD.json'
$artifactPrivacyGuard = if (Test-Path -LiteralPath $artifactPrivacyGuardPath) { Get-Content -LiteralPath $artifactPrivacyGuardPath -Raw | ConvertFrom-Json } else { $null }
$diskFullFixture = $null -ne $environmentProbe -and [bool]$environmentProbe.controlledDiskFullInjectionAvailable
$targetCertificationPath = Join-Path $repoRoot 'artifacts\phase4-target-certification\PHASE4-TARGET-CERTIFICATION.json'
$targetCertification = if (Test-Path -LiteralPath $targetCertificationPath) { Get-Content -LiteralPath $targetCertificationPath -Raw | ConvertFrom-Json } else { $null }
$realDiskFullPath = if ([string]::IsNullOrWhiteSpace($DiskFullEvidencePath)) { Join-Path $outputPath 'PHASE4-DISK-FULL-PROBE.json' } else { [System.IO.Path]::GetFullPath($DiskFullEvidencePath) }
$realDiskFull = if (Test-Path -LiteralPath $realDiskFullPath) { Get-Content -LiteralPath $realDiskFullPath -Raw | ConvertFrom-Json } else { $null }
$realDiskFullHasTypedPrivacy = $null -ne $realDiskFull -and $null -ne $realDiskFull.PSObject.Properties['syntheticOnly'] -and $null -ne $realDiskFull.PSObject.Properties['containsSecrets']
$realDiskFullPass = $realDiskFullHasTypedPrivacy -and [string]$realDiskFull.classification -eq 'RealControlledDiskFull' -and [string]$realDiskFull.outcome -eq 'Pass' -and -not [bool]$realDiskFull.syntheticOnly -and -not [bool]$realDiskFull.containsSecrets
$realDiskFullRejected = $null -ne $realDiskFull -and -not $realDiskFullPass
$diskFullFixture = $diskFullFixture -or $realDiskFullPass
$targetCertificationPass = $null -ne $targetCertification -and [string]$targetCertification.outcome -eq 'Pass' -and [bool]$targetCertification.phase4ExitReady -and -not [bool]$targetCertification.containsSecrets
$syntheticEvidenceOnly = $true
$blockers = [System.Collections.Generic.List[string]]::new()
if ($buildExit -ne 0) { $blockers.Add('build-failed') }
if ($testExit -ne 0) { $blockers.Add('tests-failed') }
if ($probeExit -ne 0) { $blockers.Add('dpapi-runtime-probe-harness-failed') }
if ($killProbeExit -ne 0 -or -not $killRollback) { $blockers.Add('os-level-process-kill-rollback-not-confirmed') }
if ($environmentProbeExit -ne 0) { $blockers.Add('target-environment-probe-failed-to-run') }
if ($artifactPrivacyGuardExit -ne 0 -or $null -eq $artifactPrivacyGuard -or [string]$artifactPrivacyGuard.outcome -ne 'Pass') { $blockers.Add('phase4-artifact-privacy-guard-failed') }
if ($missingArtifacts.Count -gt 0) { $blockers.Add('required-artifact-missing') }
if ($failedArtifacts.Count -gt 0) { $blockers.Add('prior-gate-not-pass') }
if ($sourceViolations.Count -gt 0) { $blockers.Add('phase4-source-boundary-violation') }
if (-not $dpapiProbe) { $blockers.Add('dpapi-currentuser-roundtrip-not-observed-in-test-host') }
if (-not $diskFullFixture) { $blockers.Add('real-disk-full-injection-requires-target-environment-certification') }
if ($realDiskFullRejected) { $blockers.Add('real-disk-full-evidence-rejected-by-typed-privacy-contract') }
$syntheticEvidenceOnly = -not $realDiskFullPass

$items = @(
    [ordered]@{ id = 'suite.green'; required = $true; status = $(if ($testExit -eq 0 -and $testSuite -ne 'unknown') { 'Pass' } else { 'Fail' }); evidence = 'dotnet test console summary' },
    [ordered]@{ id = 'sourceBoundary'; required = $true; status = $(if ($sourceViolations.Count -eq 0) { 'Pass' } else { 'Fail' }); evidence = 'source scan' },
    [ordered]@{ id = 'priorGates.present'; required = $true; status = $(if ($missingArtifacts.Count -eq 0 -and $failedArtifacts.Count -eq 0) { 'Pass' } else { 'Fail' }); evidence = 'WP4.1-WP4.7 artifacts' },
    [ordered]@{ id = 'dpapi.currentUser.roundtrip'; required = $true; status = $(if ($dpapiProbe) { 'Pass' } elseif ($probe -and [string]$probe.failureCode -eq 'DpapiFailure') { 'PendingTargetCertification' } else { 'Inconclusive' }); evidence = 'PHASE4-DPAPI-RUNTIME-PROBE.json' },
    [ordered]@{ id = 'processKill.rollback'; required = $true; status = $(if ($killRollback) { 'Pass' } else { 'Fail' }); evidence = 'PHASE4-PROCESS-KILL-PROBE.json' },
    [ordered]@{ id = 'diskfull.synthetic.beforeCommit'; required = $true; status = $(if ($failedArtifacts.Count -eq 0) { 'Pass' } else { 'Fail' }); classification = 'SyntheticInjection'; evidence = 'PHASE4-FAULT-GATE.json' },
    [ordered]@{ id = 'diskfull.controlled.real'; required = $true; status = $(if ($realDiskFullPass) { 'Pass' } elseif ($realDiskFullRejected) { 'Fail' } else { 'PendingTargetCertification' }); evidence = $(if ($null -ne $realDiskFull) { 'PHASE4-DISK-FULL-PROBE.json' } else { $null }) },
    [ordered]@{ id = 'plaintext.canary'; required = $true; status = $(if ($failedArtifacts.Count -eq 0) { 'Pass' } else { 'Fail' }); evidence = 'PHASE4-FAULT-GATE.json' }
)
$pendingItems = @($items | Where-Object { $_.required -and $_.status -ne 'Pass' } | ForEach-Object { $_.id })
$hasItemFail = @($items | Where-Object { $_.required -and $_.status -eq 'Fail' }).Count -gt 0
$allItemsPass = $pendingItems.Count -eq 0
$outcome = if ($hasItemFail) { 'Fail' } elseif ($allItemsPass -and $blockers.Count -eq 0) { 'Pass' } else { 'Inconclusive' }
$phase4Exit = $outcome -eq 'Pass'

$cleanedDotnetProcessIds = [System.Collections.Generic.List[int]]::new()
$leakedDotnetProcessIds = [System.Collections.Generic.List[int]]::new()
$currentDotnet = @(Get-Process dotnet -ErrorAction SilentlyContinue)
foreach ($process in $currentDotnet) {
    $isNew = $baselineDotnetIds -notcontains [int]$process.Id
    $isOwnedWindowless = $isNew -and [string]::IsNullOrEmpty($process.MainWindowTitle)
    $startedDuringRun = $false
    try { $startedDuringRun = $process.StartTime -ge $runnerStart } catch { }
    if (-not ($isOwnedWindowless -and $startedDuringRun)) { continue }
    try {
        Stop-Process -Id $process.Id -Force -ErrorAction Stop
        $cleanedDotnetProcessIds.Add([int]$process.Id)
    }
    catch { $leakedDotnetProcessIds.Add([int]$process.Id) }
}

$record = [ordered]@{
    schemaVersion = 2
    experimentId = 'PHASE4-EXIT-GATE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE4_WP48_FINAL_EXIT_REVIEW'
    outcome = $outcome
    phase4Exit = $phase4Exit
    workPackage = 'WP4.8'
    buildExitCode = $buildExit
    testExitCode = $testExit
    dpapiProbeExitCode = $probeExit
    dpapiProbeResultExitCode = $(if ($null -ne $probe -and $null -ne $probe.probeExitCode) { [int]$probe.probeExitCode } else { $null })
    dpapiProbeExecutionStatus = $(if ($null -ne $probe) { [string]$probe.executionStatus } else { $null })
    processKillProbeExitCode = $killProbeExit
    targetEnvironmentProbeExitCode = $environmentProbeExit
    targetEnvironmentProfileLoadState = $(if ($null -ne $environmentProbe -and $null -ne $environmentProbe.profile) { [string]$environmentProbe.profile.loadState } else { $null })
    targetEnvironmentProfileLoadStateQueryAvailable = $(if ($null -ne $environmentProbe -and $null -ne $environmentProbe.profile) { [bool]$environmentProbe.profile.loadStateQueryAvailable } else { $false })
    artifactPrivacyGuardExitCode = $artifactPrivacyGuardExit
    artifactPrivacyGuardArtifact = 'artifacts\phase4-artifact-privacy-guard\PHASE4-ARTIFACT-PRIVACY-GUARD.json'
    artifactPrivacyGuardOutcome = $(if ($null -ne $artifactPrivacyGuard) { [string]$artifactPrivacyGuard.outcome } else { $null })
    testSuite = $testSuite
    requiredArtifacts = $requiredArtifacts
    missingArtifacts = $missingArtifacts
    failedArtifacts = $failedArtifacts
    sourceFilesScanned = $sourceFiles.Count
    sourceViolations = $sourceViolations
    dpapiCurrentUserRoundTripObserved = $dpapiProbe
    dpapiRuntimeProbeArtifact = 'PHASE4-DPAPI-RUNTIME-PROBE.json'
    processKillProbeArtifact = 'PHASE4-PROCESS-KILL-PROBE.json'
    processKillRollbackPreservedOldRow = $killRollback
    targetEnvironmentProbeArtifact = 'PHASE4-TARGET-ENVIRONMENT-PROBE.json'
    targetCertificationArtifact = $(if ($null -ne $targetCertification) { 'artifacts\phase4-target-certification\PHASE4-TARGET-CERTIFICATION.json' } else { $null })
    targetCertificationOutcome = $(if ($null -ne $targetCertification) { [string]$targetCertification.outcome } else { $null })
    targetCertificationPass = $targetCertificationPass
    controlledDiskFullInjectionAvailable = $diskFullFixture
    realDiskFullEvidenceSupplied = ($null -ne $realDiskFull)
    realDiskFullEvidenceAccepted = $realDiskFullPass
    ownedDotnetProcessCleanup = [ordered]@{
        baselineCount = $baselineDotnetIds.Count
        cleanedCount = $cleanedDotnetProcessIds.Count
        leakedCount = $leakedDotnetProcessIds.Count
        leakedProcessIds = @($leakedDotnetProcessIds)
    }
    realDiskFullProbeArtifact = $(if ($null -ne $realDiskFull) { 'PHASE4-DISK-FULL-PROBE.json' } else { $null })
    syntheticEvidenceOnly = $syntheticEvidenceOnly
    blockers = $blockers
    items = $items
    pendingItems = $pendingItems
    previewCopyRestoreEnabled = $false
    contentSafetyAudit = [ordered]@{
        rawTargetTextCaptured = $false
        rawDynamicUiaStringsLogged = $false
        clipboardRead = $false
        clipboardWritten = $false
        networkTextSent = $false
        testDataClass = 'SyntheticCanaryOnly'
    }
}
$recordPath = Join-Path $outputPath 'PHASE4-EXIT-GATE.json'
$record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 exit gate: $($record.outcome) ($recordPath)"
if ($record.outcome -ne 'Pass') { exit 2 }
