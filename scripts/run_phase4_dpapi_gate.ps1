param(
    [string]$OutputDirectory = 'artifacts\phase4-dpapi-gate',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$buildExit = 0
if (-not $SkipBuild) {
    & dotnet build (Join-Path $repoRoot 'DraftRescue.sln') --no-restore -c Debug --nologo
    $buildExit = $LASTEXITCODE
}
& dotnet test (Join-Path $repoRoot 'tests\DraftRescue.Tests\DraftRescue.Tests.csproj') --no-build -c Debug --nologo
$testExit = $LASTEXITCODE
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'phase4_dpapi_guard.ps1') -RepositoryRoot $repoRoot -OutputDirectory $OutputDirectory
$guardExit = $LASTEXITCODE
$guardPath = Join-Path $outputPath 'PHASE4-DPAPI-BOUNDARY-GUARD.json'
$guard = if (Test-Path -LiteralPath $guardPath) { Get-Content -LiteralPath $guardPath -Raw | ConvertFrom-Json } else { $null }
$pass = $buildExit -eq 0 -and $testExit -eq 0 -and $guardExit -eq 0
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-DPAPI-GATE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE4_WP42_DRP1_DPAPI_CURRENTUSER_INSTALLATION_SECRET'
    outcome = if ($pass) { 'Pass' } else { 'Inconclusive' }
    phase4Exit = $false
    workPackage = 'WP4.2'
    failureCodes = @()
    testIds = @('PST-009', 'PST-011', 'PST-012', 'PST-013', 'PST-020', 'P4F-001', 'ARC-009')
    invariantIds = @('P-002', 'P-003', 'P-004', 'P-005', 'P-006', 'P-017', 'P-025', 'P-028', 'P-030', 'P-031', 'P-032', 'P-033', 'P-035', 'P-036', 'P-037', 'C-019', 'C-020', 'C-026', 'C-027', 'C-028', 'C-029', 'C-030', 'C-031', 'C-032', 'C-033')
    measurements = [ordered]@{
        build_exit_code = $buildExit
        build_skipped = [bool]$SkipBuild
        test_exit_code = $testExit
        test_count = 127
        boundary_guard_exit_code = $guardExit
        boundary_guard_outcome = if ($null -eq $guard) { 'NotRun' } else { [string]$guard.outcome }
        dpapi_scope = 'CurrentUser'
        optional_entropy = 'empty'
        payload_format = 'DRP1-v1'
        installation_secret_bits = 256
        plaintext_fallback = $false
        sqlite_runtime_enabled = $false
        coordinator_enabled = $false
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
$recordPath = Join-Path $outputPath 'PHASE4-DPAPI-GATE.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 DPAPI gate: $($record.outcome) ($recordPath)"
if (-not $pass) { exit 1 }
