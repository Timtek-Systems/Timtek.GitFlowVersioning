[CmdletBinding()]
param(
    [string]$RepoRoot = (Get-Location).Path,
    [string]$PackageId = 'Timtek.GitFlowVersion.Tool',
    [string]$Configuration = 'Debug',
    [string]$ToolProjectPath = '',
    [string]$PackageSource = ''
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-DotNetCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$SuppressOutput,
        [switch]$AllowFailure
    )

    try {
        if ($SuppressOutput) {
            & dotnet @Arguments *> $null
        }
        else {
            & dotnet @Arguments | Out-Host
        }
    }
    catch {
        if ($AllowFailure) {
            return
        }

        throw
    }

    if (-not $AllowFailure -and $LASTEXITCODE -ne 0) {
        $joinedArguments = [string]::Join(' ', $Arguments)
        throw "dotnet command failed: dotnet $joinedArguments"
    }
}

function Resolve-ToolProjectPath {
    param(
        [string]$Root,
        [string]$ProjectPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
        if (-not (Test-Path -LiteralPath $ProjectPath)) {
            throw "Tool project not found: $ProjectPath"
        }

        return (Resolve-Path -LiteralPath $ProjectPath).Path
    }

    $defaultProject = [System.IO.Path]::Combine($Root, 'src', 'Timtek.GitFlowVersion.Tool', 'Timtek.GitFlowVersion.Tool.csproj')
    if (-not (Test-Path -LiteralPath $defaultProject)) {
        throw "Tool project not found: $defaultProject"
    }

    return $defaultProject
}

function Resolve-PackageArtifact {
    param(
        [string]$Id,
        [string]$ProjectFile,
        [string]$BuildConfiguration,
        [string]$ExplicitSource
    )

    $sourceDirectory = ''

    if (-not [string]::IsNullOrWhiteSpace($ExplicitSource)) {
        if (-not (Test-Path -LiteralPath $ExplicitSource)) {
            throw "Package source not found: $ExplicitSource"
        }

        $sourceDirectory = (Resolve-Path -LiteralPath $ExplicitSource).Path
    }
    else {
        Write-Host "Packing tool project to produce build artifact package ($BuildConfiguration)..."
        Invoke-DotNetCommand -Arguments @('pack', $ProjectFile, '-c', $BuildConfiguration, '--nologo')

        $projectDirectory = Split-Path -Path $ProjectFile -Parent
        $sourceDirectory = [System.IO.Path]::Combine($projectDirectory, 'bin', $BuildConfiguration)

        if (-not (Test-Path -LiteralPath $sourceDirectory)) {
            throw "Expected build output directory not found: $sourceDirectory"
        }
    }

    $pattern = [System.IO.Path]::Combine($sourceDirectory, "$Id*.nupkg")
    $packages = Get-ChildItem -Path $pattern -File -ErrorAction SilentlyContinue |
        Where-Object { -not $_.Name.EndsWith('.symbols.nupkg', [System.StringComparison]::OrdinalIgnoreCase) } |
        Sort-Object LastWriteTimeUtc -Descending

    if ($packages.Count -eq 0) {
        throw "No package matching '$Id*.nupkg' found in: $sourceDirectory"
    }

    $packageFile = $packages[0]
    $prefix = "$Id."
    if (-not $packageFile.BaseName.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Cannot infer package version from file name: $($packageFile.Name)"
    }

    $packageVersion = $packageFile.BaseName.Substring($prefix.Length)

    return [pscustomobject]@{
        SourceDirectory = $sourceDirectory
        Version = $packageVersion
        PackageFile = $packageFile.FullName
    }
}

$resolvedRepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$resolvedProject = Resolve-ToolProjectPath -Root $resolvedRepoRoot -ProjectPath $ToolProjectPath
$artifact = Resolve-PackageArtifact -Id $PackageId -ProjectFile $resolvedProject -BuildConfiguration $Configuration -ExplicitSource $PackageSource

$resolvedSource = $artifact.SourceDirectory
$resolvedVersion = $artifact.Version

Write-Host "Using package source: $resolvedSource"
Write-Host "Using package version: $resolvedVersion"

Write-Host 'Uninstalling global tool (if installed)...'
Invoke-DotNetCommand -Arguments @('tool', 'uninstall', '--global', $PackageId) -SuppressOutput -AllowFailure

Write-Host 'Uninstalling local tool installs from manifests under repo (if present)...'
$manifestPattern = [System.IO.Path]::Combine($resolvedRepoRoot, '**', 'dotnet-tools.json')
$manifests = Get-ChildItem -Path $manifestPattern -File -ErrorAction SilentlyContinue
foreach ($manifest in $manifests) {
    Invoke-DotNetCommand -Arguments @('tool', 'uninstall', $PackageId, '--tool-manifest', $manifest.FullName) -SuppressOutput -AllowFailure
}

Write-Host 'Installing global tool from local build output...'
Invoke-DotNetCommand -Arguments @('tool', 'install', '--global', $PackageId, '--version', $resolvedVersion, '--source', $resolvedSource, '--ignore-failed-sources')

Write-Host 'Verifying invocation via dotnet subcommand...'
try {
    Invoke-DotNetCommand -Arguments @('gitflowversion', '--version')
}
catch {
    Write-Warning "Installed successfully, but verification failed in this environment: $($_.Exception.Message)"
    Write-Warning 'If shim execution is restricted, run verification in an unrestricted shell.'
}

Write-Host ''
Write-Host 'Done.'
