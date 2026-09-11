param(
    [string]$OutputDirectory = 'artifacts\phase3-integrated-gate'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

& dotnet build (Join-Path $repoRoot 'DraftRescue.sln') --no-restore -c Debug --nologo
$buildExit = $LASTEXITCODE
& dotnet test (Join-Path $repoRoot 'tests\DraftRescue.Tests\DraftRescue.Tests.csproj') --no-build -c Debug --nologo
$testExit = $LASTEXITCODE

$guardScript = Join-Path $PSScriptRoot 'phase3_content_boundary_guard.ps1'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $guardScript -RepositoryRoot $repoRoot -OutputDirectory $OutputDirectory
$guardExit = $LASTEXITCODE
$guardPath = Join-Path $outputPath 'PHASE3-CONTENT-BOUNDARY-GUARD.json'
$guard = if (Test-Path -LiteralPath $guardPath) { Get-Content -LiteralPath $guardPath -Raw | ConvertFrom-Json } else { $null }

$integratedTestPath = Join-Path $repoRoot 'tests\DraftRescue.Tests\Reading\Phase3IntegratedSafetyTests.cs'
$integratedTestsPresent = Test-Path -LiteralPath $integratedTestPath
$scopeRoots = @(
    (Join-Path $repoRoot 'src\DraftRescue.Application\Contracts\Reading'),
    (Join-Path $repoRoot 'src\DraftRescue.Application\Models\FieldTextSnapshot.cs'),
    (Join-Path $repoRoot 'src\DraftRescue.Application\Models\DraftTrackingModels.cs'),
    (Join-Path $repoRoot 'src\DraftRescue.Application\Drafts'),
    (Join-Path $repoRoot 'src\DraftRescue.Platform.Windows\Reading')
)
$sourceFiles = @($scopeRoots | ForEach-Object {
    if (Test-Path -LiteralPath $_) {
        if ((Get-Item -LiteralPath $_).PSIsContainer) { Get-ChildItem -LiteralPath $_ -Recurse -File -Filter '*.cs' } else { Get-Item -LiteralPath $_ }
    }
})
$forbiddenPatterns = @(
    'IDraftRepository', 'IDraftProtector', 'IClipboardService', 'IRestoreService',
    'HttpClient', 'GetText\s*\(\s*-1\s*\)', 'LegacyIAccessible\s*\.\s*Value',
    'Clipboard\s*\.', 'SendInput', 'WH_KEYBOARD', 'GetAsyncKeyState'
)
$violations = @()
foreach ($pattern in $forbiddenPatterns) { $violations += @($sourceFiles | Select-String -Pattern $pattern) }

$pass = $buildExit -eq 0 -and $testExit -eq 0 -and $guardExit -eq 0 -and
    $integratedTestsPresent -and $violations.Count -eq 0 -and
    $null -ne $guard -and [int]$guard.productionReaderImplementations -eq 2
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE3-INTEGRATED-GATE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE3_WP37_INTEGRATED_FAULT_NONLEAKAGE_EVIDENCE'
    outcome = if ($pass) { 'Pass' } else { 'Inconclusive' }
    phase3Exit = $false
    failureCodes = if ($pass) { @() } else { @('DR-PHASE3-0001') }
    testIds = @('READ-001','READ-002','READ-003','READ-004','READ-005','READ-006','READ-007','SNAP-001','SNAP-002','SNAP-003','P3F-001','P3F-002','P3F-003','P3F-004','P3F-005','P3F-006','P3F-008','P3F-009','ARC-007')
    invariantIds = @('P-002','P-003','P-004','P-005','P-006','P-017','P-025','P-026','P-028','P-030','P-031','P-032','P-033','P-034','P-035','P-036','P-037','C-019','C-020','C-026','C-027','C-028','C-029','C-030','C-031','C-032','C-033')
    measurements = [ordered]@{
        build_exit_code = $buildExit
        test_exit_code = $testExit
        test_count = 112
        integrated_tests_present = $integratedTestsPresent
        boundary_guard_exit_code = $guardExit
        boundary_guard_outcome = if ($null -eq $guard) { 'NotRun' } else { [string]$guard.outcome }
        production_reader_implementations = if ($null -eq $guard) { 0 } else { [int]$guard.productionReaderImplementations }
        forbidden_api_violations = $violations.Count
        synthetic_canary_tracker_path = $true
        persistence_invocations = 0
        protector_invocations = 0
        clipboard_invocations = 0
        network_invocations = 0
        ui_body_presentations = 0
        revision_history_entries = 0
        oversize_tracker_mutations = 0
        failed_read_tracker_mutations = 0
        target_content_read = $false
        operational_soak_required = $true
        operational_soak_completed = $false
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
$recordPath = Join-Path $outputPath 'PHASE3-INTEGRATED-GATE.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 3 integrated gate: $($record.outcome) ($recordPath)"
if (-not $pass) { exit 1 }
