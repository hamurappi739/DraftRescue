param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-contracts-gate'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$application = Join-Path $RepositoryRoot 'src\DraftRescue.Application'
$infrastructure = Join-Path $RepositoryRoot 'src\DraftRescue.Infrastructure'
$repositoryFiles = @(Get-ChildItem -LiteralPath $application -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -match 'Contracts\\Persistence\\IProtectedDraftRepository\.cs$' })
$schemaFiles = @(Get-ChildItem -LiteralPath $infrastructure -Recurse -File -Filter 'SqliteSchemaV1.cs')
$repositoryText = ($repositoryFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
$schemaText = ($schemaFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"

$violations = [System.Collections.Generic.List[string]]::new()
foreach ($token in @('FieldTextSnapshot', 'DraftPlaintextPayload', 'string Text', 'Unprotect', 'GetAllDecrypted')) {
    if ($repositoryText -match [regex]::Escape($token)) { $violations.Add("repository:$token") }
}
foreach ($token in @('WAL', 'plaintext', 'preview', 'snippet', 'raw_url', 'window_title', 'keystroke')) {
    if ($schemaText -match [regex]::Escape($token)) { $violations.Add("schema:$token") }
}
if ($schemaText -notmatch 'WITHOUT ROWID') { $violations.Add('schema:without-rowid-missing') }
if ($schemaText -notmatch 'protected_payload BLOB NOT NULL') { $violations.Add('schema:protected-payload-missing') }

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-CONTRACTS-BOUNDARY-GUARD'
    outcome = if ($violations.Count -eq 0) { 'Pass' } else { 'Inconclusive' }
    phase4Exit = $false
    workPackage = 'WP4.1'
    repositoryContractFiles = $repositoryFiles.Count
    schemaContractFiles = $schemaFiles.Count
    forbiddenViolations = $violations
    plaintextRepositoryAccepted = $false
    runtimePersistenceEnabled = $false
    dpapiEnabled = $false
}
$recordPath = Join-Path $outputPath 'PHASE4-CONTRACTS-BOUNDARY-GUARD.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 contracts guard: $($record.outcome) ($recordPath)"
if ($violations.Count -gt 0) { exit 1 }
