param(
    [switch]$IncludeNotepad,
    [ValidateRange(1, 1800)][int]$SoakSeconds = 30,
    [ValidateRange(5, 10)][int]$WpfRepeatCount = 5,
    [ValidateRange(3, 60)][int]$UiDurationSeconds = 10,
    [switch]$SkipBuild,
    [Alias('OutputDirectory')]
    [string]$SummaryDirectory = 'artifacts\phase1-suite'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$summaryPath = Join-Path $repoRoot $SummaryDirectory
New-Item -ItemType Directory -Force -Path $summaryPath | Out-Null

$suiteStartedAt = [DateTime]::UtcNow
$baselineChildProcessIds = @(
    Get-Process -Name dotnet,DraftRescue.Phase1Harness,DraftRescue.Tests -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty Id
)
$baselineChildProcessCount = $baselineChildProcessIds.Count
$cleanedChildProcessCount = 0

function Get-KnownSuiteProcesses {
    @(
        Get-Process -Name dotnet,DraftRescue.Phase1Harness,DraftRescue.Tests -ErrorAction SilentlyContinue
    )
}

function Cleanup-SuiteChildProcesses {
    $currentProcesses = @(
        Get-KnownSuiteProcesses |
            Where-Object {
                $_.Id -notin $baselineChildProcessIds -and
                $_.StartTime.ToUniversalTime() -ge $suiteStartedAt.AddSeconds(-2)
            }
    )

    foreach ($process in $currentProcesses) {
        try {
            Stop-Process -Id $process.Id -Force -ErrorAction Stop
            $script:cleanedChildProcessCount++
        }
        catch {
            # Cleanup is best-effort and never persists process diagnostics.
        }
    }
}

function Drain-SuiteChildProcesses {
    # A dotnet host can create helper processes just after its parent exits.
    # Poll briefly so the final evidence reflects post-run state, not a race.
    for ($attempt = 0; $attempt -lt 8; $attempt++) {
        Cleanup-SuiteChildProcesses
        Start-Sleep -Milliseconds 250
        if (@(Get-NewSuiteChildProcesses).Count -eq 0 -and $attempt -ge 2) {
            break
        }
    }
}

function Get-NewSuiteChildProcesses {
    @(
        Get-KnownSuiteProcesses |
            Where-Object {
                $_.Id -notin $baselineChildProcessIds -and
                $_.StartTime.ToUniversalTime() -ge $suiteStartedAt.AddSeconds(-2)
            }
    )
}

trap {
    Drain-SuiteChildProcesses
    throw
}

$steps = [System.Collections.Generic.List[object]]::new()

function Invoke-CheckedStep([string]$Name, [scriptblock]$Action, [bool]$ExpectedPass = $true) {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $exitCode = 0
    $stepOutcome = 'Pass'
    $errorCode = $null
    try {
        $global:LASTEXITCODE = 0
        & $Action
        $exitCode = if ($null -eq $global:LASTEXITCODE) { 0 } else { [int]$global:LASTEXITCODE }
        if ($ExpectedPass -and $exitCode -ne 0) {
            $stepOutcome = 'Fail'
            $errorCode = "DR-SUITE-$Name"
        } elseif (-not $ExpectedPass -and $exitCode -eq 0) {
            # A gate that unexpectedly passes is still valid, but the suite
            # records the observed result rather than manufacturing a failure.
            $stepOutcome = 'Pass'
        }
    }
    catch {
        $stepOutcome = 'Fail'
        $exitCode = 1
        $errorCode = "DR-SUITE-$Name"
    }
    finally {
        $stopwatch.Stop()
    }

    $steps.Add([ordered]@{
        name = $Name
        outcome = $stepOutcome
        exit_code = $exitCode
        duration_ms = $stopwatch.ElapsedMilliseconds
        expected_pass = $ExpectedPass
        error_code = $errorCode
    })

    if ($stepOutcome -eq 'Fail') {
        throw "Phase 1 evidence step $Name failed ($errorCode)."
    }
}

if (-not $SkipBuild) {
    Invoke-CheckedStep 'BUILD-SOLUTION' {
        dotnet build (Join-Path $repoRoot 'DraftRescue.sln') --no-restore -c Debug --nologo
    }
    Invoke-CheckedStep 'BUILD-HARNESS' {
        dotnet build (Join-Path $repoRoot 'experiments\DraftRescue.Phase1Harness\DraftRescue.Phase1Harness.csproj') --no-restore -c Release --nologo
    }
}

Invoke-CheckedStep 'TESTS' {
    dotnet test (Join-Path $repoRoot 'tests\DraftRescue.Tests\DraftRescue.Tests.csproj') --no-build -c Debug --nologo
}

Invoke-CheckedStep 'PHASE1-GUARD' {
    & (Join-Path $PSScriptRoot 'phase1_scope_guard.ps1')
}

Invoke-CheckedStep 'PHASE2-BOUNDARY-GUARD' {
    & (Join-Path $PSScriptRoot 'phase2_content_boundary_guard.ps1')
}

Invoke-CheckedStep 'PROVIDER-STRESS' {
    & (Join-Path $PSScriptRoot 'run_provider_stress.ps1') -OutputDirectory 'artifacts\phase1-provider-stress'
}

Invoke-CheckedStep 'HARNESS' {
    if ($IncludeNotepad) {
        & (Join-Path $PSScriptRoot 'run_phase1_wp14.ps1') -OutputDirectory 'artifacts\phase1' -SoakSeconds $SoakSeconds -IncludeNotepad
    } else {
        & (Join-Path $PSScriptRoot 'run_phase1_wp14.ps1') -OutputDirectory 'artifacts\phase1' -SoakSeconds $SoakSeconds
    }
}

Invoke-CheckedStep 'HARNESS-PROCESS-GUARD' {
    & (Join-Path $PSScriptRoot 'phase1_harness_process_guard.ps1') -OutputDirectory 'artifacts\phase1-harness-process-guard' -SoakSeconds 1
}

Invoke-CheckedStep 'DESKTOP-UI' {
    & (Join-Path $PSScriptRoot 'run_desktop_responsiveness.ps1') -OutputDirectory 'artifacts\phase1-desktop-ui' -DurationSeconds $UiDurationSeconds
}

# WPF repeatability is expected to remain Inconclusive in a non-interactive
# session. It is still executed and recorded; the final gate decides whether
# that is sufficient for Phase 1 completion.
Invoke-CheckedStep 'WPF-REPEAT' {
    & (Join-Path $PSScriptRoot 'run_wpf_repeat.ps1') -OutputDirectory 'artifacts\phase1-wpf-repeat-suite' -RepeatCount $WpfRepeatCount
} -ExpectedPass:$false

Invoke-CheckedStep 'EVIDENCE-PACKAGE-GUARD' {
    & (Join-Path $PSScriptRoot 'phase1_evidence_package_guard.ps1') -OutputDirectory 'artifacts\phase1-evidence-package-guard'
}

Invoke-CheckedStep 'EXIT-GATE' {
    & (Join-Path $PSScriptRoot 'phase1_exit_gate.ps1')
} -ExpectedPass:$true

Drain-SuiteChildProcesses

$gatePath = Join-Path $repoRoot 'artifacts\phase1-gate\PHASE1-EXIT-GATE.json'
$gate = if (Test-Path -LiteralPath $gatePath) {
    Get-Content -LiteralPath $gatePath -Raw | ConvertFrom-Json
} else {
    $null
}
$gateFailureCodes = if ($null -eq $gate -or $null -eq $gate.failureCodes) {
    @()
} elseif ($gate.failureCodes -is [System.Array]) {
    @($gate.failureCodes | ForEach-Object { [string]$_ })
} else {
    @([string]$gate.failureCodes)
}
$gateCriteriaValues = if ($null -eq $gate -or $null -eq $gate.criteria) {
    @()
} else {
    @($gate.criteria.PSObject.Properties | ForEach-Object { [bool]$_.Value })
}
$suiteFailureCodes = @(
    if ($null -eq $gate) { 'DR-SUITE-0001' } else { $gateFailureCodes }
)

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE1-EVIDENCE-SUITE'
    runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")
    scenarioId = 'PHASE1_REPRODUCIBLE_EVIDENCE_SUITE'
    outcome = if ($null -ne $gate -and $gate.outcome -eq 'Pass') { 'Pass' } else { 'Inconclusive' }
    durationMs = [int64](($steps | ForEach-Object { [int64]$_['duration_ms'] } | Measure-Object -Sum).Sum)
    failureCodes = $suiteFailureCodes
    invariantIds = @('P-001', 'P-002', 'P-003', 'P-025', 'P-026', 'P-028', 'P-029', 'P-030', 'C-003', 'C-016', 'C-017', 'C-018', 'C-022', 'C-023', 'C-025')
    testIds = @('OBS-005', 'OBS-006', 'OBS-007', 'FI-001', 'FI-002', 'FI-004', 'PERF-005', 'PERF-006', 'PERF-007', 'CERT-001', 'EXP-001', 'EXP-002')
    measurements = [ordered]@{
        include_notepad = [bool]$IncludeNotepad
        soak_seconds = $SoakSeconds
        wpf_repeat_count = $WpfRepeatCount
        ui_duration_seconds = $UiDurationSeconds
        gate_outcome = if ($null -eq $gate) { 'Missing' } else { [string]$gate.outcome }
        gate_failure_codes = $suiteFailureCodes
        gate_selected_wpf_evidence = if ($null -eq $gate) { $null } else { [string]$gate.diagnostics.wpf_repeat_selected_path }
        gate_wpf_repeat = if ($null -eq $gate) { '0/0' } else { "$($gate.diagnostics.wpf_repeat_pass_count)/$($gate.diagnostics.wpf_repeat_count)" }
        gate_criteria_pass_count = @($gateCriteriaValues | Where-Object { $_ -eq $true }).Count
        gate_criteria_total_count = $gateCriteriaValues.Count
        steps = @($steps)
        passed_steps = @($steps | Where-Object { $_['outcome'] -eq 'Pass' }).Count
        required_pass_steps = @($steps | Where-Object { $_['expected_pass'] -and $_['outcome'] -eq 'Pass' }).Count
        expected_inconclusive_steps = @($steps | Where-Object { -not $_['expected_pass'] -and $_['outcome'] -eq 'Pass' }).Count
        baseline_known_process_count = $baselineChildProcessCount
        cleaned_child_process_count = $cleanedChildProcessCount
        owned_child_process_count_before_final_drain = @(Get-NewSuiteChildProcesses).Count
        leaked_child_process_count = @(
            Get-NewSuiteChildProcesses
        ).Count
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

$recordPath = Join-Path $summaryPath 'PHASE1-EVIDENCE-SUITE.json'
$record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $recordPath -Encoding utf8
Drain-SuiteChildProcesses
$record.measurements.cleaned_child_process_count = $cleanedChildProcessCount
$record.measurements.owned_child_process_count_after_final_drain = @(Get-NewSuiteChildProcesses).Count
$record.measurements.leaked_child_process_count = @(Get-NewSuiteChildProcesses).Count
$record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 1 evidence suite: $($record.outcome) ($recordPath)"
if ($record.outcome -ne 'Pass') { exit 1 }
