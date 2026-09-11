param(
    [string]$OutputDirectory = 'artifacts\phase1-wpf-interactive'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$harnessPath = Join-Path $repoRoot 'experiments\DraftRescue.Phase1Harness\bin\Release\net8.0-windows\DraftRescue.Phase1Harness.exe'
if (-not (Test-Path -LiteralPath $harnessPath)) {
    throw 'Harness executable was not found. Build DraftRescue.Phase1Harness in Release first.'
}

& $harnessPath $outputPath '--wpf-interactive'
if ($LASTEXITCODE -ne 0) {
    throw "Interactive WPF probe exited with code $LASTEXITCODE."
}

$recordPath = Join-Path $outputPath 'WP14-WPF-INTERACTIVE.json'
if (-not (Test-Path -LiteralPath $recordPath)) {
    throw 'Interactive WPF probe did not write WP14-WPF-INTERACTIVE.json.'
}

$record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
$audit = $record.contentSafetyAudit
if ($record.schemaVersion -ne 1 -or
    $audit.rawTargetTextCaptured -ne $false -or
    $audit.rawDynamicUiaStringsLogged -ne $false -or
    $audit.clipboardRead -ne $false -or
    $audit.clipboardWritten -ne $false -or
    $audit.networkTextSent -ne $false) {
    throw 'Interactive WPF probe content-safety audit failed.'
}

Write-Output "Interactive WPF result: $($record.outcome) ($recordPath)"
if ($record.outcome -ne 'Pass') { exit 1 }
