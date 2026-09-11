param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-handoff-consistency'
)

$ErrorActionPreference = 'Stop'
$rootFull = [System.IO.Path]::GetFullPath($RepositoryRoot)
$outputPath = Join-Path $rootFull $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$findings = [System.Collections.Generic.List[object]]::new()

function Add-Finding([string]$code, [string]$field) {
    $findings.Add([ordered]@{ code = $code; field = $field })
}

function Read-Json([string]$relativePath) {
    $path = Join-Path $rootFull $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Add-Finding 'MissingArtifact' $relativePath; return $null }
    try { return (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json) }
    catch { Add-Finding 'MalformedJson' $relativePath; return $null }
}

$status = Read-Json 'CODEX_HANDOFF_STATUS.json'
$exit = Read-Json 'artifacts\phase4-exit-gate\PHASE4-EXIT-GATE.json'
$target = Read-Json 'artifacts\phase4-target-certification\PHASE4-TARGET-CERTIFICATION.json'
$privacy = Read-Json 'artifacts\phase4-artifact-privacy-guard\PHASE4-ARTIFACT-PRIVACY-GUARD.json'

if ($null -ne $status -and $null -ne $exit) {
    $statusExit = $status.latestPhase4ExitGate
    if ([string]$statusExit.outcome -ne [string]$exit.outcome) { Add-Finding 'StatusMismatch' 'latestPhase4ExitGate.outcome' }
    if ([string]$statusExit.testSuite -ne [string]$exit.testSuite) { Add-Finding 'StatusMismatch' 'latestPhase4ExitGate.testSuite' }
    if ((@($statusExit.blockers) -join '|') -ne (@($exit.blockers) -join '|')) { Add-Finding 'StatusMismatch' 'latestPhase4ExitGate.blockers' }
    if ((@($statusExit.pendingItems) -join '|') -ne (@($exit.pendingItems) -join '|')) { Add-Finding 'StatusMismatch' 'latestPhase4ExitGate.pendingItems' }
}
if ($null -ne $status -and $null -ne $target) {
    if ([string]$status.latestPhase4ExitGate.targetCertificationOutcome -ne [string]$target.outcome) { Add-Finding 'StatusMismatch' 'latestPhase4ExitGate.targetCertificationOutcome' }
}
if ($null -ne $privacy -and [string]$privacy.outcome -ne 'Pass') { Add-Finding 'PrivacyGuardNotPass' 'PHASE4-ARTIFACT-PRIVACY-GUARD.outcome' }

$inventoryPath = Join-Path $rootFull 'FOUNDATION_FILE_INVENTORY.txt'
$checksumPath = Join-Path $rootFull 'FOUNDATION_SHA256SUMS.txt'
$actualFiles = @(Get-ChildItem -LiteralPath $rootFull -Recurse -File | Where-Object {$_.FullName -ne $checksumPath}).Count
$actualMarkdown = @(Get-ChildItem -LiteralPath $rootFull -Recurse -File -Filter '*.md').Count
$actualChecksums = if (Test-Path -LiteralPath $checksumPath) { @(Get-Content -LiteralPath $checksumPath).Count } else { 0 }
if ($null -ne $status) {
    if ([int]$status.packageInventory.fileCount -ne $actualFiles) { Add-Finding 'InventoryMismatch' 'packageInventory.fileCount' }
    if ([int]$status.packageInventory.markdownFileCount -ne $actualMarkdown) { Add-Finding 'InventoryMismatch' 'packageInventory.markdownFileCount' }
    if ([int]$status.packageInventory.checksumEntries -ne $actualChecksums) { Add-Finding 'InventoryMismatch' 'packageInventory.checksumEntries' }
}

$outcome = if ($findings.Count -eq 0) { 'Pass' } else { 'Fail' }
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-HANDOFF-CONSISTENCY-GUARD'
    outcome = $outcome
    findings = @($findings)
    actualFileCount = $actualFiles
    actualMarkdownFileCount = $actualMarkdown
    actualChecksumEntries = $actualChecksums
    containsSecrets = $false
    rawContentExported = $false
}
$recordPath = Join-Path $outputPath 'PHASE4-HANDOFF-CONSISTENCY.json'
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 handoff consistency guard: $outcome (findings=$($findings.Count))"
if ($outcome -ne 'Pass') { exit 2 }
