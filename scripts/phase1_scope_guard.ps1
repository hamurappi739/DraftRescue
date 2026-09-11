$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceRoots = @(
    (Join-Path $repoRoot 'src\DraftRescue.Application'),
    (Join-Path $repoRoot 'src\DraftRescue.Platform.Windows'),
    (Join-Path $repoRoot 'tests\DraftRescue.Tests')
)

# UI Automation metadata APIs are allowed in Phase 1. Target content and later
# phase capabilities remain structurally forbidden.
$forbiddenPatterns = @(
    'SetWindowsHookEx',
    'WH_KEYBOARD',
    'RawInput',
    'ValuePattern\s*\.\s*Current\s*\.\s*Value',
    'TextPatternRange.*GetText',
    'DocumentRange\s*\.\s*GetText',
    'LegacyIAccessible.*Current\s*\.\s*Value',
    'Clipboard\s*\.',
    'Microsoft\.Data\.Sqlite',
    'System\.Data\.SQLite',
    'ProtectedData',
    'HttpClient',
    'TelemetryClient',
    '\.Current\s*\.\s*(Name|HelpText|ItemStatus)'
)

$files = Get-ChildItem -Path $sourceRoots -Recurse -File -Include *.cs,*.csproj
$violations = @()
foreach ($pattern in $forbiddenPatterns) {
    $matches = $files | Select-String -Pattern $pattern
    if ($matches) { $violations += $matches }
}

if ($violations.Count -gt 0) {
    Write-Host 'Phase 1 scope guard failed. Forbidden later-phase/content API detected:' -ForegroundColor Red
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'Phase 1 scope guard passed.'
