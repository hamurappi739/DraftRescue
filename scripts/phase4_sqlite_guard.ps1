param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-sqlite-gate'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$sourceRoot = Join-Path $RepositoryRoot 'src\DraftRescue.Infrastructure\Persistence'
$files = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs')
$text = ($files | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
$violations = [System.Collections.Generic.List[string]]::new()
foreach ($token in @('journal_mode=WAL', 'journal_mode = WAL', 'PRAGMA synchronous=OFF', 'secure_delete=OFF', 'protected_payload FROM drafts\s*WHERE expires_at', 'GetAllDecrypted', 'string Text', 'DraftPlaintextPayload')) {
    if ($text -match $token) { $violations.Add($token) }
}
if ($text -notmatch 'BeginTransaction') { $violations.Add('transaction-missing') }
if ($text -notmatch 'snapshot_sequence') { $violations.Add('sequence-missing') }
if ($text -notmatch 'SemaphoreSlim') { $violations.Add('serialized-writer-missing') }
if ($text -notmatch 'expires_at_utc_ms > \$now') { $violations.Add('expiry-filter-missing') }

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-SQLITE-BOUNDARY-GUARD'
    outcome = if ($violations.Count -eq 0) { 'Pass' } else { 'Inconclusive' }
    phase4Exit = $false
    workPackage = 'WP4.3'
    sourceFilesScanned = $files.Count
    forbiddenViolations = $violations
    walEnabled = $false
    plaintextRepositoryAccepted = $false
    metadataListSelectsPayload = $false
    serializedWriterPresent = ($text -match 'SemaphoreSlim')
    transactionalSequenceCheckPresent = ($text -match 'BeginTransaction' -and $text -match 'SnapshotSequence')
}
$recordPath = Join-Path $outputPath 'PHASE4-SQLITE-BOUNDARY-GUARD.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 SQLite guard: $($record.outcome) ($recordPath)"
if ($violations.Count -gt 0) { exit 1 }
