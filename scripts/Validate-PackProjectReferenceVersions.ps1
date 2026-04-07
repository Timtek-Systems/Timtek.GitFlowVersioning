[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [string]$Configuration = 'Release',

    [switch]$SkipClean,

    [switch]$SkipProjectReferenceDependencyValidation,

    [string]$ExpectedVersionPrefix = ''
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) {
        $joined = [string]::Join(' ', $Arguments)
        throw "dotnet command failed: dotnet $joined"
    }
}

function Resolve-ProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InputPath
    )

    if (-not (Test-Path -LiteralPath $InputPath)) {
        throw "Project file not found: $InputPath"
    }

    return (Resolve-Path -LiteralPath $InputPath).Path
}

function Get-ProjectIdentity {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsprojPath
    )

    [xml]$xml = Get-Content -LiteralPath $CsprojPath

    $packageId = $xml.Project.PropertyGroup.PackageId | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($packageId)) {
        $assemblyName = $xml.Project.PropertyGroup.AssemblyName | Select-Object -First 1
        if ([string]::IsNullOrWhiteSpace($assemblyName)) {
            $packageId = [System.IO.Path]::GetFileNameWithoutExtension($CsprojPath)
        }
        else {
            $packageId = $assemblyName
        }
    }

    return $packageId
}

function Get-ProjectReferences {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsprojPath
    )

    [xml]$xml = Get-Content -LiteralPath $CsprojPath
    $projectDirectory = Split-Path -Parent $CsprojPath

    $references = @()
    foreach ($itemGroup in $xml.Project.ItemGroup) {
        foreach ($projectReference in $itemGroup.ProjectReference) {
            if ($null -eq $projectReference.Include) {
                continue
            }

            $fullPath = [System.IO.Path]::GetFullPath((Join-Path $projectDirectory $projectReference.Include))
            if (Test-Path -LiteralPath $fullPath) {
                $references += $fullPath
            }
        }
    }

    return $references
}

function Get-ProjectClosure {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootProjectPath
    )

    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $queue = [System.Collections.Generic.Queue[string]]::new()
    $queue.Enqueue($RootProjectPath)

    while ($queue.Count -gt 0) {
        $current = $queue.Dequeue()
        if (-not $seen.Add($current)) {
            continue
        }

        foreach ($reference in Get-ProjectReferences -CsprojPath $current) {
            $queue.Enqueue($reference)
        }
    }

    return $seen
}

function Remove-BinAndObj {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.Generic.HashSet[string]]$ProjectPaths
    )

    foreach ($projectPath in $ProjectPaths) {
        $projectDirectory = Split-Path -Parent $projectPath
        $binPath = Join-Path $projectDirectory 'bin'
        $objPath = Join-Path $projectDirectory 'obj'

        if (Test-Path -LiteralPath $binPath) {
            Remove-Item -LiteralPath $binPath -Recurse -Force
        }

        if (Test-Path -LiteralPath $objPath) {
            Remove-Item -LiteralPath $objPath -Recurse -Force
        }
    }
}

function Get-LatestNuspec {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectDirectory,

        [Parameter(Mandatory = $true)]
        [string]$BuildConfiguration
    )

    $objConfigPath = Join-Path (Join-Path $ProjectDirectory 'obj') $BuildConfiguration
    if (-not (Test-Path -LiteralPath $objConfigPath)) {
        throw "Pack output directory not found: $objConfigPath"
    }

    $nuspec = Get-ChildItem -LiteralPath $objConfigPath -Filter '*.nuspec' -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $nuspec) {
        throw "No .nuspec file found in: $objConfigPath"
    }

    return $nuspec.FullName
}

function Get-NuspecMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$NuspecPath
    )

    [xml]$xml = Get-Content -LiteralPath $NuspecPath

    $packageNode = $xml.SelectSingleNode("/*[local-name()='package']")
    if ($null -eq $packageNode) {
        throw "Invalid nuspec format: $NuspecPath"
    }

    $idNode = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='id']")
    $versionNode = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']")
    $dependencyNodes = $xml.SelectNodes("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='dependencies']/*[local-name()='group']/*[local-name()='dependency'] | /*[local-name()='package']/*[local-name()='metadata']/*[local-name()='dependencies']/*[local-name()='dependency']")

    if ($null -eq $idNode -or $null -eq $versionNode) {
        throw "Unable to read nuspec metadata from: $NuspecPath"
    }

    $dependencies = @{}
    foreach ($node in $dependencyNodes) {
        $idAttribute = $node.Attributes['id']
        $versionAttribute = $node.Attributes['version']

        if ($null -eq $idAttribute -or $null -eq $versionAttribute) {
            continue
        }

        $dependencies[$idAttribute.Value] = $versionAttribute.Value
    }

    return [pscustomobject]@{
        PackageId = $idNode.InnerText
        Version = $versionNode.InnerText
        Dependencies = $dependencies
    }
}

$resolvedProjectPath = Resolve-ProjectPath -InputPath $ProjectPath
$projectDirectory = Split-Path -Parent $resolvedProjectPath

$projectClosure = Get-ProjectClosure -RootProjectPath $resolvedProjectPath
$directReferences = Get-ProjectReferences -CsprojPath $resolvedProjectPath

if (-not $SkipProjectReferenceDependencyValidation -and $directReferences.Count -eq 0) {
    throw "Project has no direct ProjectReference entries to validate: $resolvedProjectPath"
}

if ($SkipProjectReferenceDependencyValidation -and $directReferences.Count -eq 0) {
    Write-Host 'No direct ProjectReference entries found; dependency version checks will be skipped.'
}

if (-not $SkipClean) {
    Write-Host 'Cleaning bin/obj for root project and project references...'
    Remove-BinAndObj -ProjectPaths $projectClosure
}

Write-Host 'Running dotnet pack...'
Invoke-DotNet -Arguments @('pack', $resolvedProjectPath, '-c', $Configuration, '--nologo')

$nuspecPath = Get-LatestNuspec -ProjectDirectory $projectDirectory -BuildConfiguration $Configuration
$metadata = Get-NuspecMetadata -NuspecPath $nuspecPath

Write-Host ''
Write-Host "Nuspec: $nuspecPath"
Write-Host "Packed package: $($metadata.PackageId) $($metadata.Version)"

if (-not [string]::IsNullOrWhiteSpace($ExpectedVersionPrefix)) {
    if (-not $metadata.Version.StartsWith($ExpectedVersionPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Packed version '$($metadata.Version)' does not start with expected prefix '$ExpectedVersionPrefix'."
    }
}

if ($SkipProjectReferenceDependencyValidation) {
    Write-Host ''
    Write-Host 'Skipping project-reference dependency version validation as requested.'
    Write-Host 'Pack dependency versions validated successfully.' -ForegroundColor Green
    return
}

$expectedDependencyVersion = $metadata.Version
$projectReferencePackageIds = $directReferences | ForEach-Object { Get-ProjectIdentity -CsprojPath $_ }

$missingDependencies = @()
$mismatchedDependencies = @()

foreach ($packageId in $projectReferencePackageIds) {
    if (-not $metadata.Dependencies.ContainsKey($packageId)) {
        $missingDependencies += $packageId
        continue
    }

    $actualVersion = $metadata.Dependencies[$packageId]
    if ($actualVersion -ne $expectedDependencyVersion) {
        $mismatchedDependencies += [pscustomobject]@{
            PackageId = $packageId
            Expected = $expectedDependencyVersion
            Actual = $actualVersion
        }
    }
}

if ($missingDependencies.Count -gt 0) {
    Write-Host ''
    Write-Host 'Missing dependency entries for direct project references:' -ForegroundColor Yellow
    $missingDependencies | ForEach-Object { Write-Host " - $_" -ForegroundColor Yellow }
}

if ($mismatchedDependencies.Count -gt 0) {
    Write-Host ''
    Write-Host 'Dependency version mismatches:' -ForegroundColor Red
    foreach ($mismatch in $mismatchedDependencies) {
        Write-Host " - $($mismatch.PackageId): expected '$($mismatch.Expected)', actual '$($mismatch.Actual)'" -ForegroundColor Red
    }

    throw 'Pack dependency version validation failed.'
}

if ($missingDependencies.Count -gt 0) {
    throw 'Pack dependency validation failed due to missing dependency entries for direct project references.'
}

Write-Host ''
Write-Host 'Pack dependency versions validated successfully.' -ForegroundColor Green
