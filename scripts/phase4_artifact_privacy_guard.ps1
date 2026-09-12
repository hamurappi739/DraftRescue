param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = 'artifacts\phase4-artifact-privacy-guard',
    [string[]]$ArtifactDirectories = @('artifacts\phase4-exit-gate', 'artifacts\phase4-target-certification')
)

$ErrorActionPreference = 'Stop'
$rootFull = [System.IO.Path]::GetFullPath($RepositoryRoot)
$outputPath = Join-Path $rootFull $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$forbiddenPropertyNames = @(
    'capturedText', 'draftText', 'plaintextText', 'clipboardText',
    'snippet', 'textValue', 'uiaValue', 'rawValue', 'windowTitle', 'url'
)
$allowedReadinessStatuses = @('Unknown', 'PendingTargetCertification', 'Pass')
$allowedReadinessBlockers = @(
    'environment-evidence-missing', 'windows-target-required', 'user-profile-required',
    'interactive-user-session-required', 'appdata-directory-required',
    'dpapi-protect-folder-required', 'profile-state-query-unavailable',
    'profile-not-loaded', 'profile-state-unknown', 'profile-state-unrecognized',
    'temp-directory-not-writable', 'dpapi-payload-roundtrip-missing',
    'dpapi-installation-secret-roundtrip-missing'
)
$findings = [System.Collections.Generic.List[object]]::new()
$filesChecked = 0
$readinessContractsChecked = 0

function Add-Finding([string]$file, [string]$code, [string]$field) {
    $findings.Add([ordered]@{ file = $file; code = $code; field = $field })
}

function Inspect-Readiness([object]$node, [string]$file, [string]$fieldPath) {
    if ($null -eq $node) { Add-Finding $file 'MalformedReadiness' $fieldPath; return }
    $script:readinessContractsChecked++
    $statusProperty = $node.PSObject.Properties['status']
    $blockersProperty = $node.PSObject.Properties['blockers']
    if ($null -eq $statusProperty) { Add-Finding $file 'MalformedReadiness' ($fieldPath + '.status') }
    elseif ($allowedReadinessStatuses -notcontains [string]$statusProperty.Value) { Add-Finding $file 'UnknownReadinessStatus' ($fieldPath + '.status') }
    if ($null -eq $blockersProperty) {
        Add-Finding $file 'MalformedReadiness' ($fieldPath + '.blockers')
        return
    }
    $blockers = @($blockersProperty.Value)
    for ($index = 0; $index -lt $blockers.Count; $index++) {
        if ($allowedReadinessBlockers -notcontains [string]$blockers[$index]) { Add-Finding $file 'UnknownReadinessBlocker' ($fieldPath + '.blockers[' + $index + ']') }
    }
    if ($null -ne $statusProperty -and [string]$statusProperty.Value -eq 'Pass' -and $blockers.Count -ne 0) {
        Add-Finding $file 'ReadinessPassWithBlockers' ($fieldPath + '.blockers')
    }
}

function Get-RelativePath([string]$path) {
    $full = [System.IO.Path]::GetFullPath($path)
    if ($full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($rootFull.Length).TrimStart('\').Replace('/', '\')
    }
    return 'external-artifact'
}

function Inspect-Node($node, [string]$file, [string]$fieldPath) {
    if ($null -eq $node) { return }
    if ($node -is [System.Management.Automation.PSCustomObject]) {
        $classificationProperty = $node.PSObject.Properties['classification']
        if ($null -ne $classificationProperty -and [string]$classificationProperty.Value -eq 'RealControlledDiskFull') {
            if ($null -eq $node.PSObject.Properties['syntheticOnly']) { Add-Finding $file 'MissingTypedPrivacyField' ($fieldPath + '.syntheticOnly').TrimStart('.') }
            if ($null -eq $node.PSObject.Properties['containsSecrets']) { Add-Finding $file 'MissingTypedPrivacyField' ($fieldPath + '.containsSecrets').TrimStart('.') }
        }
        foreach ($property in $node.PSObject.Properties) {
            $name = [string]$property.Name
            $path = if ([string]::IsNullOrWhiteSpace($fieldPath)) { $name } else { $fieldPath + '.' + $name }
            if ($name -eq 'readiness') { Inspect-Readiness $property.Value $file $path }
            if ($name -eq 'dpapiReadinessStatus' -and $allowedReadinessStatuses -notcontains [string]$property.Value) { Add-Finding $file 'UnknownReadinessStatus' $path }
            if ($name -eq 'dpapiReadinessBlockers') {
                $flatBlockers = @($property.Value)
                for ($index = 0; $index -lt $flatBlockers.Count; $index++) {
                    if ($allowedReadinessBlockers -notcontains [string]$flatBlockers[$index]) { Add-Finding $file 'UnknownReadinessBlocker' ($path + '[' + $index + ']') }
                }
            }
            if ($forbiddenPropertyNames -contains $name) { Add-Finding $file 'ForbiddenContentProperty' $path }
            if ($name -eq 'containsSecrets' -and [bool]$property.Value) { Add-Finding $file 'SensitiveEvidenceFlag' $path }
            if ($property.Value -is [string] -and ([string]$property.Value -match '^(?i:[A-Z]:\\|\\\\)')) { Add-Finding $file 'AbsolutePathValue' $path }
            Inspect-Node $property.Value $file $path
        }
        return
    }
    if ($node -is [System.Collections.IEnumerable] -and -not ($node -is [string])) {
        $index = 0
        foreach ($item in $node) {
            Inspect-Node $item $file ($fieldPath + '[' + $index + ']')
            $index++
        }
    }
}

foreach ($directory in $ArtifactDirectories) {
    $directoryPath = Join-Path $rootFull $directory
    if (-not (Test-Path -LiteralPath $directoryPath -PathType Container)) { continue }
    foreach ($json in @(Get-ChildItem -LiteralPath $directoryPath -File -Filter '*.json')) {
        $filesChecked++
        $relative = Get-RelativePath $json.FullName
        try {
            $record = Get-Content -LiteralPath $json.FullName -Raw | ConvertFrom-Json
            Inspect-Node $record $relative ''
        }
        catch { Add-Finding $relative 'JsonParseFailure' 'document' }
    }
}

$outcome = if ($findings.Count -eq 0) { 'Pass' } else { 'Fail' }
$record = [ordered]@{
    schemaVersion = 1
    experimentId = 'PHASE4-ARTIFACT-PRIVACY-GUARD'
    outcome = $outcome
    filesChecked = $filesChecked
    readinessContractsChecked = $readinessContractsChecked
    findings = @($findings)
    containsSecrets = $false
    rawContentExported = $false
}
$recordPath = Join-Path $outputPath 'PHASE4-ARTIFACT-PRIVACY-GUARD.json'
$record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 artifact privacy guard: $outcome (files=$filesChecked)"
if ($outcome -ne 'Pass') { exit 2 }
