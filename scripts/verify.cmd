@echo off
setlocal
cd /d "%~dp0\.."

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\phase0_scope_guard.ps1 || exit /b 1
dotnet --info || exit /b 1
dotnet restore DraftRescue.sln || exit /b 1
dotnet build DraftRescue.sln --no-restore -c Debug || exit /b 1
dotnet test tests\DraftRescue.Tests\DraftRescue.Tests.csproj --no-build -c Debug || exit /b 1

echo Phase 0 verification passed.
