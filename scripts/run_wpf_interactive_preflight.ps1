param(
    [string]$OutputDirectory = 'artifacts\phase1-wpf-preflight'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$harnessPath = Join-Path $repoRoot 'experiments\DraftRescue.Phase1Harness\bin\Release\net8.0-windows\DraftRescue.Phase1Harness.exe'
if (-not (Test-Path -LiteralPath $harnessPath)) {
    throw 'Harness executable was not found. Build DraftRescue.Phase1Harness in Release first.'
}

& $harnessPath $outputPath '--wpf-preflight'
if ($LASTEXITCODE -ne 0) {
    throw "Interactive WPF preflight exited with code $LASTEXITCODE."
}

$recordPath = Join-Path $outputPath 'WP14-WPF-INTERACTIVE-PREFLIGHT.json'
if (-not (Test-Path -LiteralPath $recordPath)) {
    throw 'Interactive WPF preflight did not write its evidence record.'
}

$record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
$audit = $record.contentSafetyAudit
if ($record.schemaVersion -ne 1 -or
    $audit.rawTargetTextCaptured -ne $false -or
    $audit.rawDynamicUiaStringsLogged -ne $false -or
    $audit.clipboardRead -ne $false -or
    $audit.clipboardWritten -ne $false -or
    $audit.networkTextSent -ne $false -or
    $record.measurements.target_content_read -ne $false -or
    $record.measurements.text_reader_invocation_count -ne 0 -or
    $record.measurements.repository_invocation_count -ne 0) {
    throw 'Interactive WPF preflight content-safety audit failed.'
}

Write-Output "Interactive WPF preflight: $($record.outcome) ($recordPath)"
Write-Output "Ready for fixture: $($record.measurements.ready_for_interactive_fixture)"
