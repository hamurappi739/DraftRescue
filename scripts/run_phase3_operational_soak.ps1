param(
    [int]$DurationSeconds = 30,
    [string]$OutputDirectory = 'artifacts\phase3-soak-final'
)

$ErrorActionPreference = 'Stop'
if ($DurationSeconds -lt 1 -or $DurationSeconds -gt 1800) { throw 'DurationSeconds must be between 1 and 1800.' }
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'experiments\DraftRescue.Phase3Harness\DraftRescue.Phase3Harness.csproj'
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
& dotnet build $project --no-restore -c Release --nologo
$buildExit = $LASTEXITCODE
if ($buildExit -ne 0) { exit $buildExit }
$harness = Join-Path $repoRoot 'experiments\DraftRescue.Phase3Harness\bin\Release\net8.0-windows\DraftRescue.Phase3Harness.exe'
& $harness $outputPath --duration-seconds $DurationSeconds
$runExit = $LASTEXITCODE
$recordPath = Join-Path $outputPath 'PHASE3-OPERATIONAL-SOAK.json'
if (-not (Test-Path -LiteralPath $recordPath)) { throw 'Phase 3 soak record was not produced.' }
$record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
$record | Add-Member -NotePropertyName harness_build_exit_code -NotePropertyValue $buildExit
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 3 operational soak runner: $($record.outcome) ($recordPath)"
if ($runExit -ne 0 -or $record.outcome -ne 'Pass') { exit 1 }
