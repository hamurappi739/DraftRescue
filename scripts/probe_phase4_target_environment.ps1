param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-exit-gate'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $RepositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$tempRoot = [System.IO.Path]::GetTempPath()
$tempVolumeRoot = [System.IO.Path]::GetPathRoot($tempRoot)
$driveName = $tempVolumeRoot.TrimEnd('\').TrimEnd(':')
$drive = Get-PSDrive -Name $driveName -ErrorAction SilentlyContinue
$logicalDisk = $null
try { $logicalDisk = Get-CimInstance Win32_LogicalDisk -Filter ("DeviceID='" + $tempVolumeRoot.TrimEnd('\') + "'") -ErrorAction Stop } catch { }
$tempFreeBytes = $null
if ($null -ne $logicalDisk -and $null -ne $logicalDisk.FreeSpace) { $tempFreeBytes = [int64]$logicalDisk.FreeSpace }
elseif ($null -ne $drive -and $null -ne $drive.Free) { $tempFreeBytes = [int64]$drive.Free }
if ($null -eq $tempFreeBytes) {
    try { $tempFreeBytes = [int64]([System.IO.DriveInfo]::new($tempVolumeRoot).AvailableFreeSpace) } catch { }
}
$profilePath = [Environment]::ExpandEnvironmentVariables('%USERPROFILE%')
$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
$sid = if ($null -eq $identity.User) { $null } else { $identity.User.Value }
$sidHash8 = $null
if (-not [string]::IsNullOrWhiteSpace($sid)) {
    $sidBytes = [Text.Encoding]::UTF8.GetBytes($sid)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $sidHash8 = (($sha.ComputeHash($sidBytes)[0..3] | ForEach-Object { $_.ToString('x2') }) -join '') }
    finally { $sha.Dispose() }
}
$sessionId = [Diagnostics.Process]::GetCurrentProcess().SessionId
$userInteractive = [Environment]::UserInteractive
$isSystem = $identity.IsSystem -or ($sid -eq 'S-1-5-18')
$identityKind = if ($isSystem) { 'System' } elseif ($userInteractive) { 'InteractiveUser' } else { 'Other' }
$isElevated = (New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$appDataIsUnc = $false
try { $appDataIsUnc = (-not [string]::IsNullOrWhiteSpace($appData)) -and ([Uri]::new($appData).IsUnc) } catch { }
$protectFolder = if ([string]::IsNullOrWhiteSpace($appData)) { $false } else { Test-Path -LiteralPath (Join-Path $appData 'Microsoft\Protect') }
$profileLoaded = $false
$profileLoadState = 'Unknown'
try {
    $profileRecord = Get-CimInstance Win32_UserProfile -Filter ("SID='" + $sid + "'") -ErrorAction Stop
    if ($null -eq $profileRecord) { $profileLoadState = 'Unknown' }
    elseif ([bool]$profileRecord.Loaded) { $profileLoaded = $true; $profileLoadState = 'Loaded' }
    else { $profileLoadState = 'NotLoaded' }
}
catch { $profileLoadState = 'QueryUnavailable' }
$record = [ordered]@{
    schemaVersion = 2
    experimentId = 'PHASE4-TARGET-ENVIRONMENT-PROBE'
    syntheticOnly = $true
    windows = ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT)
    userProfileAvailable = (Test-Path -LiteralPath $profilePath -PathType Container)
    tempDirectoryWritable = $false
    tempVolumeFreeBytes = $tempFreeBytes
    controlledDiskFullInjectionAvailable = $false
    controlledDiskFullInjectionReason = 'No disposable quota or virtual-disk fixture is configured'
    session = [ordered]@{ id = $sessionId; interactive = $userInteractive; elevated = $isElevated }
    identity = [ordered]@{ kind = $identityKind; sidHash8 = $sidHash8; namesIncluded = $false }
    profile = [ordered]@{
        loaded = $profileLoaded
        loadState = $profileLoadState
        loadStateQueryAvailable = ($profileLoadState -ne 'QueryUnavailable')
        appDataPresent = (Test-Path -LiteralPath $appData -PathType Container)
        appDataIsUnc = $appDataIsUnc
        protectFolderPresent = $protectFolder
        localAppDataPresent = (Test-Path -LiteralPath $localAppData -PathType Container)
    }
}
$probeFile = Join-Path $tempRoot ('DraftRescue-EnvironmentProbe-' + [Guid]::NewGuid().ToString('N') + '.tmp')
try {
    [System.IO.File]::WriteAllBytes($probeFile, [byte[]](1, 2, 3, 4))
    $record.tempDirectoryWritable = $true
}
catch { $record.tempDirectoryWritable = $false }
finally { Remove-Item -LiteralPath $probeFile -Force -ErrorAction SilentlyContinue }
$path = Join-Path $outputPath 'PHASE4-TARGET-ENVIRONMENT-PROBE.json'
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path -Encoding utf8
Write-Output "Phase 4 target environment probe: writable=$($record.tempDirectoryWritable) diskFullFixture=$($record.controlledDiskFullInjectionAvailable) ($path)"
