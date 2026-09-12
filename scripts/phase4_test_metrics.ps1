function Invoke-Phase4TestSuite {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

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
    Start-Sleep -Milliseconds 250
    $cleanedProcessIds = [System.Collections.Generic.List[int]]::new()
    $leakedProcessIds = [System.Collections.Generic.List[int]]::new()
    foreach ($process in @(Get-Process dotnet -ErrorAction SilentlyContinue)) {
        $isNew = $baselineDotnetIds -notcontains [int]$process.Id
        $isWindowless = [string]::IsNullOrEmpty($process.MainWindowTitle)
        $startedDuringRun = $false
        try { $startedDuringRun = $process.StartTime -ge $runStart } catch { }
        if (-not ($isNew -and $isWindowless -and $startedDuringRun)) { continue }
        try {
            Stop-Process -Id $process.Id -Force -ErrorAction Stop
            $cleanedProcessIds.Add([int]$process.Id)
        }
        catch { $leakedProcessIds.Add([int]$process.Id) }
    }
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
