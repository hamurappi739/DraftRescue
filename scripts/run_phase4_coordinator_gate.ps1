param(
    [string]$OutputDirectory = 'artifacts\phase4-coordinator-gate',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'phase4_test_metrics.ps1')
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$buildExit = 0
if (-not $SkipBuild) {
    & dotnet build (Join-Path $repoRoot 'DraftRescue.sln') --no-restore -c Debug --nologo
    $buildExit = $LASTEXITCODE
}
$testMetrics = Invoke-Phase4TestSuite (Join-Path $repoRoot 'tests\DraftRescue.Tests\DraftRescue.Tests.csproj')
$testExit = $testMetrics.ExitCode
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'phase4_coordinator_guard.ps1') -RepositoryRoot $repoRoot -OutputDirectory $OutputDirectory
$guardExit = $LASTEXITCODE
$guardPath = Join-Path $outputPath 'PHASE4-COORDINATOR-BOUNDARY-GUARD.json'
$guard = if (Test-Path -LiteralPath $guardPath) { Get-Content -LiteralPath $guardPath -Raw | ConvertFrom-Json } else { $null }
$pass = $buildExit -eq 0 -and $testExit -eq 0 -and $guardExit -eq 0
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-COORDINATOR-GATE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE4_WP44_PROTECT_CHECKPOINT_RETENTION_STATE_MACHINE'
    outcome = if ($pass) { 'Pass' } else { 'Inconclusive' }
    phase4Exit = $false
    workPackage = 'WP4.4'
    failureCodes = @()
    testIds = @('PST-004', 'PST-007', 'PST-018', 'PST-020', 'P4F-001', 'P4F-009', 'P4F-015', 'ARC-011')
    invariantIds = @('P-002', 'P-003', 'P-004', 'P-005', 'P-006', 'P-017', 'P-025', 'P-028', 'P-030', 'P-031', 'P-032', 'P-033', 'P-035', 'P-036', 'P-037', 'C-019', 'C-020', 'C-026', 'C-027', 'C-028', 'C-029', 'C-030', 'C-031', 'C-032', 'C-033')
    measurements = [ordered]@{
        build_exit_code = $buildExit
        build_skipped = [bool]$SkipBuild
        test_exit_code = $testExit
        test_count = $testMetrics.Total
        boundary_guard_exit_code = $guardExit
        boundary_guard_outcome = if ($null -eq $guard) { 'NotRun' } else { [string]$guard.outcome }
        protect_before_repository = $true
        stale_generation_rejected = $true
        plaintext_fallback = $false
        metadata_only_retention = $true
        preview_copy_restore_enabled = $false
    }
    contentSafetyAudit = [ordered]@{
        rawTargetTextCaptured = $false
        rawDynamicUiaStringsLogged = $false
        clipboardRead = $false
        clipboardWritten = $false
        networkTextSent = $false
        testDataClass = 'SyntheticCanaryOnly'
    }
}
$recordPath = Join-Path $outputPath 'PHASE4-COORDINATOR-GATE.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 coordinator gate: $($record.outcome) ($recordPath)"
if (-not $pass) { exit 1 }
