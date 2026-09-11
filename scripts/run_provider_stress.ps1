param(
    [string]$OutputDirectory = "artifacts\phase1-provider-stress"
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$harnessPath = Join-Path $repoRoot 'experiments\DraftRescue.Phase1Harness\bin\Release\net8.0-windows\DraftRescue.Phase1Harness.exe'
if (-not (Test-Path -LiteralPath $harnessPath)) {
    throw 'Harness executable was not found. Build DraftRescue.Phase1Harness in Release first.'
}

& $harnessPath $outputPath '--provider-stress'
if ($LASTEXITCODE -ne 0) {
    throw "Provider stress harness exited with code $LASTEXITCODE."
}

$recordPath = Join-Path $outputPath 'WP14-PROVIDER-STRESS.json'
if (-not (Test-Path -LiteralPath $recordPath)) {
    throw 'Provider stress probe did not write WP14-PROVIDER-STRESS.json.'
}

$record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
$audit = $record.contentSafetyAudit
if ($audit.rawTargetTextCaptured -or $audit.rawDynamicUiaStringsLogged -or $audit.clipboardRead -or
    $audit.clipboardWritten -or $audit.networkTextSent -or
    $record.measurements.text_reader_invocation_count -ne 0 -or
    $record.measurements.repository_invocation_count -ne 0 -or
    $record.measurements.target_content_read) {
    throw 'Provider stress probe content-safety audit failed.'
}

Write-Output "Provider stress result: $($record.outcome) ($recordPath)"
if ($record.outcome -ne 'Pass') {
    exit 1
}
