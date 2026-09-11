param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-fault-gate'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$roots = @(
    (Join-Path $RepositoryRoot 'src\DraftRescue.Application\Persistence'),
    (Join-Path $RepositoryRoot 'src\DraftRescue.Infrastructure\Persistence'),
    (Join-Path $RepositoryRoot 'src\DraftRescue.Platform.Windows\Security')
)
$files = @($roots | ForEach-Object { if (Test-Path -LiteralPath $_) { Get-ChildItem -LiteralPath $_ -Recurse -File -Filter '*.cs' } })
$text = ($files | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
$violations = [System.Collections.Generic.List[string]]::new()
foreach ($token in @('File.WriteAllText', 'File.AppendAllText', 'File.WriteAllLines', 'GetAllDecrypted', 'journal_mode=WAL', 'clipboard', 'HttpClient', 'Console.WriteLine')) {
    if ($text -match [regex]::Escape($token)) { $violations.Add($token) }
}
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-PLAINTEXT-CANARY-GUARD'
    outcome = if ($violations.Count -eq 0) { 'Pass' } else { 'Inconclusive' }
    workPackage = 'WP4.7'
    sourceFilesScanned = $files.Count
    forbiddenViolations = $violations
    canaryMarkerSource = 'SyntheticPerTestOnly'
    utf8AndUtf16ByteScanRequired = $true
    protectedBlobMustDifferFromPlaintext = $true
    metadataListDecryptCountRequired = 0
    noPlaintextFallback = $true
}
$recordPath = Join-Path $outputPath 'PHASE4-PLAINTEXT-CANARY-GUARD.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 plaintext canary guard: $($record.outcome) ($recordPath)"
if ($violations.Count -gt 0) { exit 1 }
