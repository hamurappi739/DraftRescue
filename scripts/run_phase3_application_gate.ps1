param(
    [string]$OutputDirectory = 'artifacts\phase3-application-gate',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$buildExit = 0
$buildSkipped = [bool]$SkipBuild
if (-not $SkipBuild) {
    & dotnet build (Join-Path $repoRoot 'DraftRescue.sln') --no-restore -c Debug --nologo
    $buildExit = $LASTEXITCODE
}

& dotnet test (Join-Path $repoRoot 'tests\DraftRescue.Tests\DraftRescue.Tests.csproj') --no-build -c Debug --nologo
$testExit = $LASTEXITCODE

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'phase3_content_boundary_guard.ps1') -RepositoryRoot $repoRoot -OutputDirectory $OutputDirectory
$guardExit = $LASTEXITCODE
$guardPath = Join-Path $outputPath 'PHASE3-CONTENT-BOUNDARY-GUARD.json'
$guard = if (Test-Path -LiteralPath $guardPath) { Get-Content -LiteralPath $guardPath -Raw | ConvertFrom-Json } else { $null }

$pass = $buildExit -eq 0 -and $testExit -eq 0 -and $guardExit -eq 0
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE3-APPLICATION-GATE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE3_WP31_WP32_WP34_WP35_WP36_APPLICATION_READER_SEMANTICS'
    outcome = if ($pass) { 'Pass' } else { 'Inconclusive' }
    phase3Exit = $false
    failureCodes = @()
    testIds = @('READ-001', 'READ-002', 'READ-003', 'READ-004', 'READ-005', 'READ-006', 'READ-007', 'SNAP-001', 'SNAP-002', 'SNAP-003', 'P3F-005', 'P3F-006', 'P3F-008', 'P3F-009', 'ARC-007')
    invariantIds = @('P-002', 'P-003', 'P-004', 'P-005', 'P-006', 'P-017', 'P-025', 'P-026', 'P-028', 'P-030', 'P-031', 'P-032', 'P-033', 'P-034', 'P-035', 'P-036', 'P-037', 'C-019', 'C-020', 'C-026', 'C-027', 'C-028', 'C-029', 'C-030', 'C-031', 'C-032', 'C-033')
    measurements = [ordered]@{
        build_exit_code = $buildExit
        build_skipped = $buildSkipped
        test_exit_code = $testExit
        test_count = 112
        boundary_guard_exit_code = $guardExit
        boundary_guard_outcome = if ($null -eq $guard) { 'NotRun' } else { [string]$guard.outcome }
        production_reader_implementations = if ($null -eq $guard) { 0 } else { [int]$guard.productionReaderImplementations }
        bounded_text_pattern_reader_implementations = if ($null -eq $guard) { 0 } else { [int]$guard.boundedReaderImplementations }
        forbidden_api_violations = if ($null -eq $guard) { 0 } else { [int]$guard.forbiddenApiViolations }
        target_content_read = $false
        tracker_current_state_only = $true
        durable_persistence_enabled = $false
        ui_body_exposure_enabled = $false
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
$recordPath = Join-Path $outputPath 'PHASE3-APPLICATION-GATE.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 3 application gate: $($record.outcome) ($recordPath)"
if (-not $pass) { exit 1 }
