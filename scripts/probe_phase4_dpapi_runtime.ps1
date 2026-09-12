param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-exit-gate'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$project = Join-Path $RepositoryRoot 'experiments\DraftRescue.Phase4CrashProbe\DraftRescue.Phase4CrashProbe.csproj'
$dll = Join-Path $RepositoryRoot 'experiments\DraftRescue.Phase4CrashProbe\bin\Debug\net8.0-windows\DraftRescue.Phase4CrashProbe.dll'
$record = [ordered]@{
    schemaVersion = 3
    experimentId = 'PHASE4-DPAPI-RUNTIME-PROBE'
    targetScope = 'CurrentUser'
    syntheticOnly = $true
    available = $false
    payloadRoundTripObserved = $false
    installationSecretRoundTripObserved = $false
    failureCode = $null
    failureReason = 'Unknown'
    failureStage = 'Unknown'
    probeExitCode = $null
    harnessExitCode = 0
    executionStatus = 'NotStarted'
    environment = $null
    containsSecrets = $false
}
try {
    $record.executionStatus = 'BuildStarted'
    & dotnet build $project --no-restore -c Debug --nologo | Out-Null
    if ($LASTEXITCODE -ne 0) {
        $record.executionStatus = 'HarnessBuildFailed'
        $record.failureCode = 'HarnessBuildFailed'
        $record.harnessExitCode = 61
        throw 'probe-build-failed'
    }
    $record.executionStatus = 'ProbeStarted'
    $probeOutput = @(& dotnet $dll dpapi 2>&1)
    $record.probeExitCode = $LASTEXITCODE
    $record.available = ($LASTEXITCODE -eq 0)
    if ($record.available) {
        $record.failureCode = 'None'
        $record.payloadRoundTripObserved = $true
        $record.installationSecretRoundTripObserved = $true
        $record.failureReason = 'None'
        $record.failureStage = 'None'
        $record.executionStatus = 'ProbePass'
    }
    else {
        $record.failureCode = if ($LASTEXITCODE -eq 6) { 'DpapiFailure' } elseif ($LASTEXITCODE -eq 2) { 'ProbeArgumentOrStartupFailure' } else { 'DpapiProbeUnexpectedExit' }
        $reasonMatch = [regex]::Match(($probeOutput -join "`n"), '(?m)^failureReason=(PlatformNotSupported|Unauthorized|Cryptographic|Unknown)$')
        if ($reasonMatch.Success) { $record.failureReason = $reasonMatch.Groups[1].Value }
        $payloadMatch = [regex]::Match(($probeOutput -join "`n"), '(?m)^payloadRoundTrip=(Pass|Fail)$')
        if ($payloadMatch.Success) { $record.payloadRoundTripObserved = $payloadMatch.Groups[1].Value -eq 'Pass' }
        $secretMatch = [regex]::Match(($probeOutput -join "`n"), '(?m)^installationSecretRoundTrip=(Pass|Fail)$')
        if ($secretMatch.Success) { $record.installationSecretRoundTripObserved = $secretMatch.Groups[1].Value -eq 'Pass' }
        $stageMatch = [regex]::Match(($probeOutput -join "`n"), '(?m)^failureStage=(Protect|Unprotect|InstallationSecretCreate|InstallationSecretLoad|Validation|Unknown)$')
        if ($stageMatch.Success) { $record.failureStage = $stageMatch.Groups[1].Value }
        $record.executionStatus = if ($LASTEXITCODE -eq 6) { 'ProbeCompletedDpapiUnavailable' } else { 'ProbeCompletedUnexpectedly' }
    }
}
catch {
    if ($null -eq $record.failureCode) { $record.failureCode = 'RuntimeProbeUnavailable' }
    if ($record.executionStatus -eq 'NotStarted') { $record.executionStatus = 'HarnessFailed' }
    if ($record.harnessExitCode -eq 0) { $record.harnessExitCode = 61 }
}
$environmentPath = Join-Path $outputPath 'PHASE4-TARGET-ENVIRONMENT-PROBE.json'
if (Test-Path -LiteralPath $environmentPath) {
    try {
        $record.environment = Get-Content -LiteralPath $environmentPath -Raw | ConvertFrom-Json
    } catch {
        $record.environment = $null
        if ($record.failureCode -eq 'None') {
            $record.failureCode = 'EnvironmentArtifactInvalid'
            $record.executionStatus = 'EvidenceInvalid'
        }
    }
}
if ($null -eq $record.failureCode) { $record.failureCode = 'RuntimeProbeUnavailable' }
$path = Join-Path $outputPath 'PHASE4-DPAPI-RUNTIME-PROBE.json'
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path -Encoding utf8
Write-Output "Phase 4 DPAPI runtime probe: available=$($record.available) ($path)"
if ($record.harnessExitCode -ne 0) { exit $record.harnessExitCode }
exit 0
