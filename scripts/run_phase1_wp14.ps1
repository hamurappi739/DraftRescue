param(
    [string]$OutputDirectory = "artifacts\phase1",
    [switch]$IncludeNotepad,
    [ValidateRange(1, 1800)][int]$SoakSeconds = 30
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$harnessPath = Join-Path $repoRoot 'experiments\DraftRescue.Phase1Harness\bin\Release\net8.0-windows\DraftRescue.Phase1Harness.exe'
if (-not (Test-Path -LiteralPath $harnessPath)) {
    throw 'Harness executable was not found. Build DraftRescue.Phase1Harness in Release first.'
}

$harnessArgs = @($outputPath, '--soak-seconds', $SoakSeconds)
if ($IncludeNotepad) { $harnessArgs += '--notepad' }
& $harnessPath @harnessArgs
if ($LASTEXITCODE -ne 0) {
    throw "WP-1.4 harness failed with exit code $LASTEXITCODE."
}

$records = Get-ChildItem -LiteralPath $outputPath -Filter 'WP14-*.json' -File
if ($records.Count -lt 4 -or -not (Test-Path (Join-Path $outputPath 'WP14-SOAK.json')) -or
    -not (Test-Path (Join-Path $outputPath 'WP14-WPF.json')) -or
    ($IncludeNotepad -and -not (Test-Path (Join-Path $outputPath 'WP14-NPAD.json')))) {
    throw 'WP-1.4 harness did not produce the required experiment records.'
}

foreach ($record in $records) {
    $json = Get-Content -LiteralPath $record.FullName -Raw | ConvertFrom-Json
    if ($json.schemaVersion -ne 1 -or $json.contentSafetyAudit.rawTargetTextCaptured -ne $false -or
        $json.contentSafetyAudit.rawDynamicUiaStringsLogged -ne $false -or
        $json.contentSafetyAudit.clipboardRead -ne $false -or
        $json.contentSafetyAudit.clipboardWritten -ne $false -or
        $json.contentSafetyAudit.networkTextSent -ne $false) {
        throw "Content-safety audit failed for $($record.Name)."
    }
}

Write-Output "WP-1.4 experiment records validated: $($records.Count)."
