$script:Phase4OwnedDotnetProcessDrainMaxAttempts = 8
$script:Phase4OwnedDotnetProcessDrainIntervalMilliseconds = 250

function Get-Phase4OwnedDotnetProcesses {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [int[]]$BaselineDotnetIds,
        [Parameter(Mandatory = $true)]
        [DateTime]$RunStart
    )

    $baseline = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($id in $BaselineDotnetIds) { [void]$baseline.Add([int]$id) }
    $cutoff = $RunStart.AddSeconds(-2)
    @(
        Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object {
            if ($baseline.Contains([int]$_.Id)) { return $false }
            if (-not [string]::IsNullOrEmpty($_.MainWindowTitle)) { return $false }
            try { return $_.StartTime -ge $cutoff } catch { return $false }
        }
    )
}

function Drain-Phase4OwnedDotnetProcesses {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [int[]]$BaselineDotnetIds,
        [Parameter(Mandatory = $true)]
        [DateTime]$RunStart,
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[int]]$CleanedProcessIds,
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[int]]$LeakedProcessIds
    )

    $cleaned = [System.Collections.Generic.HashSet[int]]::new()
    $leaked = [System.Collections.Generic.HashSet[int]]::new()
    for ($attempt = 0; $attempt -lt $script:Phase4OwnedDotnetProcessDrainMaxAttempts; $attempt++) {
        foreach ($process in @(Get-Phase4OwnedDotnetProcesses -BaselineDotnetIds $BaselineDotnetIds -RunStart $RunStart)) {
            $id = [int]$process.Id
            try {
                Stop-Process -Id $id -Force -ErrorAction Stop
                if ($cleaned.Add($id)) { $CleanedProcessIds.Add($id) }
            }
            catch {
                [void]$leaked.Add($id)
            }
        }
        Start-Sleep -Milliseconds $script:Phase4OwnedDotnetProcessDrainIntervalMilliseconds
        $remaining = @(Get-Phase4OwnedDotnetProcesses -BaselineDotnetIds $BaselineDotnetIds -RunStart $RunStart)
        if ($remaining.Count -eq 0 -and $attempt -ge 2) { break }
    }

    foreach ($process in @(Get-Phase4OwnedDotnetProcesses -BaselineDotnetIds $BaselineDotnetIds -RunStart $RunStart)) {
        $id = [int]$process.Id
        if ($leaked.Add($id)) { $LeakedProcessIds.Add($id) }
    }
}

function Invoke-Phase4TestSuite {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    # Keep the test runner scoped to this invocation. Without this setting
    # MSBuild may leave windowless dotnet nodes alive and contaminate the next
    # certification baseline.
    $env:MSBUILDDISABLENODEREUSE = '1'
    $runStart = Get-Date
    $baselineDotnetIds = @(Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object { [int]$_.Id })
    $previousLanguage = $env:DOTNET_CLI_UI_LANGUAGE
    $env:DOTNET_CLI_UI_LANGUAGE = 'en'
    try {
        $lines = @(& dotnet test $ProjectPath --no-build -c Debug --nologo 2>&1)
    }
    finally {
        if ($null -eq $previousLanguage) { Remove-Item Env:DOTNET_CLI_UI_LANGUAGE -ErrorAction SilentlyContinue }
        else { $env:DOTNET_CLI_UI_LANGUAGE = $previousLanguage }
    }
    $cleanedProcessIds = [System.Collections.Generic.List[int]]::new()
    $leakedProcessIds = [System.Collections.Generic.List[int]]::new()
    Drain-Phase4OwnedDotnetProcesses -BaselineDotnetIds $baselineDotnetIds -RunStart $runStart -CleanedProcessIds $cleanedProcessIds -LeakedProcessIds $leakedProcessIds
    foreach ($line in $lines) { Write-Host $line }
    $exitCode = $LASTEXITCODE
    $text = $lines -join "`n"

    $total = $null
    $passed = $null
    $totalMatch = [regex]::Match($text, '(?i)total(?: tests?)?\s*[:=]\s*(\d+)')
    if ($totalMatch.Success) { $total = [int]$totalMatch.Groups[1].Value }

    $passedMatches = [regex]::Matches($text, '(?i)passed\s*[:=]\s*(\d+)')
    if ($passedMatches.Count -gt 0) { $passed = [int]$passedMatches[$passedMatches.Count - 1].Groups[1].Value }

    [pscustomobject]@{
        ExitCode = $exitCode
        Passed = $passed
        Total = $total
        Suite = if ($null -ne $passed -and $null -ne $total) { "$passed/$total" } else { 'unknown' }
        CleanedProcessCount = $cleanedProcessIds.Count
        LeakedProcessCount = $leakedProcessIds.Count
        LeakedProcessIds = @($leakedProcessIds)
    }
}
