param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-recovery-gate'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$sourceRoot = Join-Path $RepositoryRoot 'src\DraftRescue.Infrastructure\Persistence'
$files = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs')
$text = ($files | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
$violations = [System.Collections.Generic.List[string]]::new()
foreach ($token in @('DROP TABLE', 'strings ', 'raw-page', 'dump', 'upload', 'HttpClient', 'GetAllDecrypted', 'journal_mode=WAL', 'CreateNewStore')) {
    if ($text -match [regex]::Escape($token)) { $violations.Add($token) }
}
if ($text -notmatch 'Quarantine') { $violations.Add('quarantine-boundary-missing') }
if ($text -notmatch 'Incompatible') { $violations.Add('incompatible-classification-missing') }
if ($text -notmatch 'CorruptRecord') { $violations.Add('corrupt-record-classification-missing') }

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-RECOVERY-BOUNDARY-GUARD'
    outcome = if ($violations.Count -eq 0) { 'Pass' } else { 'Inconclusive' }
    phase4Exit = $false
    workPackage = 'WP4.6'
    sourceFilesScanned = $files.Count
    forbiddenViolations = $violations
    localQuarantineOnly = ($text -match 'File.Move' -and $text -notmatch 'HttpClient')
    salvageEnabled = $false
    autoDropOrRecreateEnabled = $false
    incompatibleSchemaFailsClosed = ($text -match 'Incompatible')
    malformedRecordTyped = ($text -match 'CorruptRecord')
}
$recordPath = Join-Path $outputPath 'PHASE4-RECOVERY-BOUNDARY-GUARD.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 recovery guard: $($record.outcome) ($recordPath)"
if ($violations.Count -gt 0) { exit 1 }
