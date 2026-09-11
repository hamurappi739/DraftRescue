$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceRoots = @(
    (Join-Path $repoRoot 'src'),
    (Join-Path $repoRoot 'tests')
)

$forbiddenPatterns = @(
    'SetWindowsHookEx',
    'WH_KEYBOARD',
    'KeyboardHook',
    'RawInput',
    'System\.Windows\.Automation',
    'UIAutomationClient',
    'Microsoft\.Data\.Sqlite',
    'System\.Data\.SQLite',
    'Clipboard\.',
    'HttpClient',
    'TelemetryClient'
)

$files = Get-ChildItem -Path $sourceRoots -Recurse -File -Include *.cs,*.csproj
$violations = @()

foreach ($pattern in $forbiddenPatterns) {
    $matches = $files | Select-String -Pattern $pattern
    if ($matches) {
        $violations += $matches
    }
}

if ($violations.Count -gt 0) {
    Write-Host 'Phase 0 scope guard failed. Later-phase API/code detected:' -ForegroundColor Red
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'Phase 0 scope guard passed.'
