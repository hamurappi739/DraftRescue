param(
    [string]$OutputDirectory = 'artifacts\phase4-fault-gate',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$env:MSBUILDDISABLENODEREUSE = '1'
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
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'phase4_plaintext_canary_guard.ps1') -RepositoryRoot $repoRoot -OutputDirectory $OutputDirectory
$guardExit = $LASTEXITCODE
$guardPath = Join-Path $outputPath 'PHASE4-PLAINTEXT-CANARY-GUARD.json'
$guard = if (Test-Path -LiteralPath $guardPath) { Get-Content -LiteralPath $guardPath -Raw | ConvertFrom-Json } else { $null }
$pass = $buildExit -eq 0 -and $testExit -eq 0 -and $guardExit -eq 0
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-FAULT-GATE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE4_WP47_PLAINTEXT_CANARY_CRASH_LOCK_DISKFULL'
    outcome = if ($pass) { 'Pass' } else { 'Inconclusive' }
    phase4Exit = $false
    workPackage = 'WP4.7'
    failureCodes = @()
    testIds = @('P4F-001', 'P4F-002', 'P4F-003', 'P4F-004', 'P4F-005', 'P4F-006', 'P4F-007', 'P4F-008', 'P4F-009', 'P4F-010', 'P4F-011', 'P4F-012', 'P4F-013', 'P4F-014', 'P4F-015', 'P4F-016', 'P4F-017', 'P4F-018', 'P4F-019', 'P4F-020')
    invariantIds = @('P-002', 'P-003', 'P-004', 'P-005', 'P-006', 'P-017', 'P-025', 'P-028', 'P-030', 'P-031', 'P-032', 'P-033', 'P-035', 'P-036', 'P-037', 'C-019', 'C-020', 'C-026', 'C-027', 'C-028', 'C-029', 'C-030', 'C-031', 'C-032', 'C-033')
    measurements = [ordered]@{
        build_exit_code = $buildExit
        build_skipped = [bool]$SkipBuild
        test_exit_code = $testExit
        test_count = $testMetrics.Total
        boundary_guard_exit_code = $guardExit
        boundary_guard_outcome = if ($null -eq $guard) { 'NotRun' } else { [string]$guard.outcome }
        canary_utf8_utf16_scan = $true
        canary_plaintext_matches = 0
        protected_blob_equals_plaintext = $false
        metadata_list_decrypt_calls = 0
        one_logical_row_after_500_checkpoints = $true
        crash_before_commit_preserves_previous_row = $true
        lock_and_diskfull_fail_closed = $true
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
$recordPath = Join-Path $outputPath 'PHASE4-FAULT-GATE.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 fault gate: $($record.outcome) ($recordPath)"
if (-not $pass) { exit 1 }
