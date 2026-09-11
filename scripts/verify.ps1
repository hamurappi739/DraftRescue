$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    function Invoke-NativeChecked {
        param(
            [Parameter(Mandatory = $true)][string]$Description,
            [Parameter(Mandatory = $true)][scriptblock]$Command
        )

        Write-Host "==> $Description"
        $global:LASTEXITCODE = 0
        & $Command
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "$Description failed with exit code $exitCode."
        }
    }

    Write-Host '==> Phase 0 scope guard'
    $global:LASTEXITCODE = 0
    & .\scripts\phase0_scope_guard.ps1
    if ($LASTEXITCODE -ne 0) {
        throw "Phase 0 scope guard failed with exit code $LASTEXITCODE."
    }

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'dotnet was not found. Install a Windows x64 .NET 8 SDK before running Phase 0 verification.'
    }

    Invoke-NativeChecked 'dotnet --info' { dotnet --info }

    Write-Host '==> Checking for a compatible .NET 8 SDK'
    $global:LASTEXITCODE = 0
    $installedSdks = @(dotnet --list-sdks)
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet --list-sdks failed with exit code $LASTEXITCODE."
    }

    $net8Sdks = @($installedSdks | Where-Object { $_ -match '^8\.0\.' })
    if ($net8Sdks.Count -eq 0) {
        throw 'No .NET 8 SDK is installed. A .NET runtime alone is insufficient for restore/build/test.'
    }

    Write-Host ('Compatible .NET 8 SDK(s): ' + ($net8Sdks -join ', '))

    Invoke-NativeChecked 'Restoring solution' { dotnet restore .\DraftRescue.sln }
    Invoke-NativeChecked 'Building solution' { dotnet build .\DraftRescue.sln --no-restore -c Debug }
    Invoke-NativeChecked 'Running tests' { dotnet test .\tests\DraftRescue.Tests\DraftRescue.Tests.csproj --no-build -c Debug }

    Write-Host '==> Phase 0 verification passed.' -ForegroundColor Green
}
catch {
    Write-Host ('==> Phase 0 verification FAILED: ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
