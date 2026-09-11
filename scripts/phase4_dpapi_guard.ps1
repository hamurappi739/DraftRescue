param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-dpapi-gate'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$sourceRoot = Join-Path $RepositoryRoot 'src\DraftRescue.Platform.Windows\Security'
$files = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs')
$text = ($files | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
$violations = [System.Collections.Generic.List[string]]::new()
foreach ($token in @('DataProtectionScope.LocalMachine', 'LocalMachine', 'machine name', 'MachineName', 'Environment.MachineName', 'username as entropy', 'plaintext fallback', 'WriteAllText', 'GetAllDecrypted')) {
    if ($text -match [regex]::Escape($token)) { $violations.Add($token) }
}
if ($text -notmatch 'DataProtectionScope\.CurrentUser') { $violations.Add('CurrentUser-missing') }
if ($text -notmatch 'Array\.Empty<byte>\(\)') { $violations.Add('empty-entropy-not-explicit') }
if ($text -notmatch 'CryptographicOperations\.ZeroMemory') { $violations.Add('buffer-zeroing-missing') }

$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-DPAPI-BOUNDARY-GUARD'
    outcome = if ($violations.Count -eq 0) { 'Pass' } else { 'Inconclusive' }
    phase4Exit = $false
    workPackage = 'WP4.2'
    sourceFilesScanned = $files.Count
    forbiddenViolations = $violations
    dpapiScopeCurrentUserOnly = ($text -match 'DataProtectionScope\.CurrentUser' -and $text -notmatch 'LocalMachine')
    emptyOptionalEntropy = ($text -match 'Array\.Empty<byte>\(\)')
    plaintextFallback = $false
    plaintextBuffersZeroed = ($text -match 'CryptographicOperations\.ZeroMemory')
}
$recordPath = Join-Path $outputPath 'PHASE4-DPAPI-BOUNDARY-GUARD.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 DPAPI guard: $($record.outcome) ($recordPath)"
if ($violations.Count -gt 0) { exit 1 }
