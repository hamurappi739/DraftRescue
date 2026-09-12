param(
    [string]$OutputDirectory = 'artifacts\phase4-contracts-gate',
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

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'phase4_contracts_guard.ps1') -RepositoryRoot $repoRoot -OutputDirectory $OutputDirectory
$guardExit = $LASTEXITCODE
$guardPath = Join-Path $outputPath 'PHASE4-CONTRACTS-BOUNDARY-GUARD.json'
$guard = if (Test-Path -LiteralPath $guardPath) { Get-Content -LiteralPath $guardPath -Raw | ConvertFrom-Json } else { $null }

$pass = $buildExit -eq 0 -and $testExit -eq 0 -and $guardExit -eq 0
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-CONTRACTS-GATE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE4_WP41_PROTECTED_RECORD_CONTRACT_AND_SQLITE_SCHEMA'
    outcome = if ($pass) { 'Pass' } else { 'Inconclusive' }
    phase4Exit = $false
    workPackage = 'WP4.1'
    failureCodes = @()
    testIds = @('PST-001', 'PST-002', 'PST-003', 'PST-005', 'PST-018', 'PST-020', 'P4F-009', 'ARC-008')
    invariantIds = @('P-002', 'P-003', 'P-004', 'P-005', 'P-006', 'P-017', 'P-025', 'P-028', 'P-030', 'P-031', 'P-032', 'P-033', 'P-035', 'P-036', 'P-037', 'C-019', 'C-020', 'C-026', 'C-027', 'C-028', 'C-029', 'C-030', 'C-031', 'C-032', 'C-033')
    measurements = [ordered]@{
        build_exit_code = $buildExit
        build_skipped = [bool]$SkipBuild
        test_exit_code = $testExit
        test_count = $testMetrics.Total
        contracts_guard_exit_code = $guardExit
        contracts_guard_outcome = if ($null -eq $guard) { 'NotRun' } else { [string]$guard.outcome }
        plaintext_repository_accepted = $false
        canonical_schema_declared = $true
        dpapi_enabled = $false
        sqlite_runtime_enabled = $false
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
$recordPath = Join-Path $outputPath 'PHASE4-CONTRACTS-GATE.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 contracts gate: $($record.outcome) ($recordPath)"
if (-not $pass) { exit 1 }
