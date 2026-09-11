param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-exit-gate'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $RepositoryRoot 'experiments\DraftRescue.Phase4CrashProbe\DraftRescue.Phase4CrashProbe.csproj'
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$work = Join-Path ([System.IO.Path]::GetTempPath()) ('DraftRescue-WP48-kill-' + [Guid]::NewGuid().ToString('N'))
$database = Join-Path $work 'drafts.db'
$ready = Join-Path $work 'ready.signal'
$probe = [ordered]@{ schemaVersion = 1; experimentId = 'PHASE4-PROCESS-KILL-PROBE'; syntheticOnly = $true; outcome = 'Inconclusive'; childKilled = $false; rollbackPreservedOldRow = $false; verificationExitCode = $null }
try {
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    & dotnet build $project --no-restore -c Debug --nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'probe-build-failed' }
    $dll = Join-Path $RepositoryRoot 'experiments\DraftRescue.Phase4CrashProbe\bin\Debug\net8.0-windows\DraftRescue.Phase4CrashProbe.dll'
    & dotnet $dll seed $database | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'probe-seed-failed' }
    $childArguments = '"' + $dll + '" child "' + $database + '" "' + $ready + '"'
    $child = Start-Process -FilePath 'dotnet' -ArgumentList $childArguments -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    while (-not (Test-Path -LiteralPath $ready) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
    if (-not (Test-Path -LiteralPath $ready)) { throw 'probe-ready-timeout' }
    Stop-Process -Id $child.Id -Force
    $probe.childKilled = $true
    $child.WaitForExit()
    & dotnet $dll verify $database | Out-Null
    $probe.verificationExitCode = $LASTEXITCODE
    $probe.rollbackPreservedOldRow = ($LASTEXITCODE -eq 0)
    if ($probe.rollbackPreservedOldRow) { $probe.outcome = 'Pass' }
}
catch { $probe.failureCode = 'ProcessKillProbeUnavailable' }
finally {
    $path = Join-Path $outputPath 'PHASE4-PROCESS-KILL-PROBE.json'
    $probe | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path -Encoding utf8
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }
}
Write-Output "Phase 4 process-kill probe: $($probe.outcome)"
if ($probe.outcome -ne 'Pass') { exit 2 }
