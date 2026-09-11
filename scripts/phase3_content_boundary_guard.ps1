param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase3-application-gate'
)

$ErrorActionPreference = 'Stop'
$roots = @(
    (Join-Path $RepositoryRoot 'src\DraftRescue.Application\Contracts\Reading'),
    (Join-Path $RepositoryRoot 'src\DraftRescue.Application\Models\FieldTextSnapshot.cs'),
    (Join-Path $RepositoryRoot 'src\DraftRescue.Application\Models\DraftTrackingModels.cs'),
    (Join-Path $RepositoryRoot 'src\DraftRescue.Application\Drafts'),
    (Join-Path $RepositoryRoot 'src\DraftRescue.Platform.Windows')
)
$files = @($roots | ForEach-Object {
    if (Test-Path -LiteralPath $_) {
        if ((Get-Item -LiteralPath $_).PSIsContainer) {
            Get-ChildItem -LiteralPath $_ -Recurse -File -Filter '*.cs'
        }
        else {
            Get-Item -LiteralPath $_
        }
    }
})

$patterns = @(
    'GetText\s*\(\s*-1\s*\)',
    'LegacyIAccessible\s*\.\s*Value',
    'Clipboard\s*\.',
    'GetAsyncKeyState',
    'WH_KEYBOARD',
    'SendInput',
    'IDraftRepository',
    'IDraftProtector',
    'IClipboardService',
    'IRestoreService'
)
$violations = @()
foreach ($pattern in $patterns) {
    $violations += @($files | Select-String -Pattern $pattern)
}

$readerImplementations = @($files | Select-String -Pattern 'class\s+\w*TextReader\b|class\s+\w*Reader\s*:\s*IEligibleFieldTextReader')
$pass = $violations.Count -eq 0
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE3-CONTENT-BOUNDARY-GUARD'
    outcome = if ($pass) { 'Pass' } else { 'Fail' }
    sourceFilesScanned = $files.Count
    forbiddenApiViolations = $violations.Count
    productionReaderImplementations = $readerImplementations.Count
    boundedReaderImplementations = @($readerImplementations | Where-Object { $_.Line -match 'TextPatternBoundedReader' }).Count
    targetContentRead = $false
    contentSafetyAudit = [ordered]@{
        rawTargetTextCaptured = $false
        rawDynamicUiaStringsLogged = $false
        clipboardRead = $false
        clipboardWritten = $false
        networkTextSent = $false
        testDataClass = 'MetadataOnly'
    }
}
$recordPath = Join-Path (Join-Path $RepositoryRoot $OutputDirectory) 'PHASE3-CONTENT-BOUNDARY-GUARD.json'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $recordPath) | Out-Null
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $recordPath -Encoding utf8
if ($pass) {
    Write-Output 'Phase 3 content-boundary guard passed.'
    exit 0
}

    Write-Output "Phase 3 content-boundary guard failed ($($violations.Count) forbidden APIs, $($readerImplementations.Count) reader implementations)."
exit 1
