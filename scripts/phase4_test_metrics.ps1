function Invoke-Phase4TestSuite {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $previousLanguage = $env:DOTNET_CLI_UI_LANGUAGE
    $env:DOTNET_CLI_UI_LANGUAGE = 'en'
    try {
        $lines = @(& dotnet test $ProjectPath --no-build -c Debug --nologo 2>&1)
    }
    finally {
        if ($null -eq $previousLanguage) { Remove-Item Env:DOTNET_CLI_UI_LANGUAGE -ErrorAction SilentlyContinue }
        else { $env:DOTNET_CLI_UI_LANGUAGE = $previousLanguage }
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
    }
}
