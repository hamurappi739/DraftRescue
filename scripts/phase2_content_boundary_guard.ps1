$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceRoots = @(
    (Join-Path $repoRoot 'src\DraftRescue.Application'),
    (Join-Path $repoRoot 'src\DraftRescue.Platform.Windows')
)

# This guard is intended for Phase 2 only. Phase 3 will intentionally add an
# eligible content reader and must replace this gate with a capability-aware one.
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

$files = Get-ChildItem -Path $sourceRoots -Recurse -File -Include *.cs
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
