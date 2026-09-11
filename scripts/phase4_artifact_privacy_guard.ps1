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
$findings = [System.Collections.Generic.List[object]]::new()
$filesChecked = 0

function Add-Finding([string]$file, [string]$code, [string]$field) {
    $findings.Add([ordered]@{ file = $file; code = $code; field = $field })
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
    findings = @($findings)
    containsSecrets = $false
    rawContentExported = $false
}
$recordPath = Join-Path $outputPath 'PHASE4-ARTIFACT-PRIVACY-GUARD.json'
$record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Phase 4 artifact privacy guard: $outcome (files=$filesChecked)"
if ($outcome -ne 'Pass') { exit 2 }
