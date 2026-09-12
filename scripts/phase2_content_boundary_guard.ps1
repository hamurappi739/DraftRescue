param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
$sourceRoots = @(
    (Join-Path $repoRoot 'src\DraftRescue.Application\Security'),
    (Join-Path $repoRoot 'src\DraftRescue.Platform.Windows\Security')
)

# Phase 3 is allowed to add content readers under Platform.Windows/Reading. Keep
# this guard focused on the Phase 2 security components so the historical gate
# remains useful without treating the authorized Phase 3 reader as a violation.
$forbiddenPatterns = @(
    'ValuePattern\s*\.\s*Value',
    'DocumentRange\s*\.\s*GetText',
    'TextPatternRange.*GetText',
    'LegacyIAccessible.*Value',
    'Clipboard\s*\.',
    'GetAsyncKeyState',
    'WH_KEYBOARD',
    'WH_KEYBOARD_LL',
    'IEligibleFieldTextReader\s+[A-Za-z_]'
)

$files = @($sourceRoots | ForEach-Object {
    if (Test-Path -LiteralPath $_) {
        Get-ChildItem -LiteralPath $_ -Recurse -File -Filter '*.cs'
    }
})
$violations = @()
foreach ($pattern in $forbiddenPatterns) {
    $matches = $files | Select-String -Pattern $pattern
    if ($matches) { $violations += $matches }
}

if ($violations.Count -gt 0) {
    Write-Host 'Phase 2 content-boundary guard failed:' -ForegroundColor Red
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'Phase 2 content-boundary guard passed.'
