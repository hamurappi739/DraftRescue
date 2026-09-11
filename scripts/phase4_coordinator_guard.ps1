param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-coordinator-gate'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$applicationRoot = Join-Path $RepositoryRoot 'src\DraftRescue.Application\Persistence'
$infrastructureRoot = Join-Path $RepositoryRoot 'src\DraftRescue.Infrastructure\Persistence'
$files = @(Get-ChildItem -LiteralPath $applicationRoot,$infrastructureRoot -Recurse -File -Filter '*.cs')
$text = ($files | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
$violations = [System.Collections.Generic.List[string]]::new()
foreach ($token in @('SaveTypedText', 'WritePlaintext', 'GetAllDecrypted', 'Preview', 'Clipboard', 'Restore', 'TextPattern.GetText', 'journal_mode=WAL')) {
    if ($text -match [regex]::Escape($token)) { $violations.Add($token) }
}
if ($text -notmatch 'IDraftProtector') { $violations.Add('protector-boundary-missing') }
if ($text -notmatch 'IProtectedDraftRepository') { $violations.Add('repository-boundary-missing') }
if ($text -notmatch 'DeleteExpiredAsync') { $violations.Add('retention-delete-missing') }
if ($text -notmatch 'ContextGeneration') { $violations.Add('generation-check-missing') }

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-COORDINATOR-BOUNDARY-GUARD'
    outcome = if ($violations.Count -eq 0) { 'Pass' } else { 'Inconclusive' }
    phase4Exit = $false
    workPackage = 'WP4.4'
    sourceFilesScanned = $files.Count
    forbiddenViolations = $violations
    protectBeforeRepository = ($text -match 'IDraftProtector' -and $text -match 'IProtectedDraftRepository')
    staleGenerationRejected = ($text -match 'ContextGeneration')
    plaintextFallback = $false
    retentionMetadataOnly = ($text -match 'DeleteExpiredAsync')
    previewCopyRestoreEnabled = $false
}
$recordPath = Join-Path $outputPath 'PHASE4-COORDINATOR-BOUNDARY-GUARD.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 coordinator guard: $($record.outcome) ($recordPath)"
if ($violations.Count -gt 0) { exit 1 }
