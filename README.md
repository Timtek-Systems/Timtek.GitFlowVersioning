# Timtek.GitFlowVersioning

A minimal, opinionated GitFlow-focussed versioning utility for .NET projects that automatically computes semantic version numbers from your Git history.

## Overview

This project provides two complementary tools:

1. **Timtek.GitFlowVersioning** — An MSBuild task for automatic versioning in .NET projects
2. **Timtek.GitFlowVersion.Tool** — A standalone CLI tool for computing versions from Git history

Both tools automatically compute semantic version numbers from your Git history during the build or on-demand, so you never need to maintain version numbers by hand.

## What It Does

`Timtek.GitFlowVersioning` reads your branch name and the most recent version tag to produce a fully populated set of version properties including `Version`, `PackageVersion`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion`.

This solution is deliberately constrained in scope. It is designed to work for:

- Standard *GitFlow* workflows using `main`, `develop`, `release/*`, and `hotfix/*` branches
- .NET SDK-style projects using MSBuild version properties and the `dotnet` CLI
- TeamCity and GitHub Actions CI environments

This supports our approach to versioning and release management across our projects without the overhead and complexity of more widely-scoped tools. If your workflow or requirements differ, there are other excellent Git-based versioning tools available that may be a better fit.

## How It Works

### MSBuild Task

The MSBuild package ships as a development dependency containing an MSBuild task. At build time the task:

1. Inspects the current Git branch and the most recent `git describe` tag
2. Classifies the branch using GitFlow conventions (`main`, `develop`, `release/*`, `hotfix/*`, or feature/other)
3. Computes a [SemVer 2.0](https://semver.org/) version based on the branch type and the number of commits since the last tag
4. Sets the standard MSBuild version properties so the compiler, assembly info, and NuGet pack all receive the correct version automatically
5. Generates a `GitVersionInformation` class in the intermediate output containing the full set of version variables. Because this class does not exist until compilation, IDE tooling may report errors and direct access would require reflection. The `GitVersion` class in the `TA.Utils.Core` NuGet package provides a safe, easy-to-use wrapper for runtime access to these values

### Branch Versioning Strategy

| Branch | Pre-release label | Example |
|---|---|---|
| `main` / `master` | *(none — stable release)* | `1.2.3` |
| `develop` | `alpha` | `1.3.0-alpha.12` |
| `release/*` | `beta` | `1.3.0-beta.4` |
| `hotfix/*` | `beta` | `1.2.4-beta.1` |
| Any other (`feature/*`, etc.) | `alpha` | `1.3.0-alpha.7` |

The base version is taken from the most recent Git tag matching `*.*.*` (with or without a `v` prefix). The commit distance from that tag is used as the pre-release number or added to the patch component on `main`.

## Getting Started

### MSBuild Package

Install the package into your project:

```shell
dotnet add package Timtek.GitFlowVersioning
```

That's it. The next `dotnet build` or `dotnet pack` will automatically compute and apply versions. No configuration is required for standard GitFlow workflows.

### CLI Tool

Install as a local tool (recommended for team projects):

```shell
dotnet tool install Timtek.GitFlowVersion.Tool
```

Or install globally:

```shell
dotnet tool install --global Timtek.GitFlowVersion.Tool
```

## Usage

### MSBuild Package

Once installed, automatic versioning is enabled by default. No additional configuration is needed.

**Tagging a Release:**

Create a tag on `main` when you want to mark a release:

```shell
git tag 1.0.0
git push origin 1.0.0
```

Tags can optionally use a `v` prefix (`v1.0.0`).

### CLI Tool

```shell
dotnet gitflowversion [path]
```

The tool outputs a JSON object containing all computed version variables:

```json
{
  "SemVer": "1.3.0-alpha.12",
  "FullSemVer": "1.3.0-alpha.12+12",
  "InformationalVersion": "1.3.0-alpha.12+12.Branch.develop.Sha.a1b2c3d...",
  "Major": "1",
  "Minor": "3",
  "Patch": "0",
  "PreReleaseLabel": "alpha",
  "PreReleaseNumber": "12",
  ...
}
```

## Accessing Version Information at Runtime

The generated `GitVersionInformation` class is internal and does not exist until compilation, so referencing it directly in your source code will produce build errors in the IDE. Accessing it via reflection is possible but clumsy and error-prone.

Instead, use the `GitVersion` class from the `TA.Utils.Core` NuGet package, which provides a safe wrapper:

```csharp
var version = GitVersion.GitInformationalVersion;
Console.WriteLine(version);  // "1.2.3+5.Branch.main.Sha.a1b2c3d..."
```

## CI Support

The task automatically emits service messages when running under supported CI environments:

- **GitHub Actions** — sets `::notice` annotations and writes `semver`, `fullSemVer`, and `informationalVersion` to `$GITHUB_OUTPUT`
- **TeamCity** — sets the build number and exposes version parameters via `##teamcity` messages

## Configuration

### Disabling Versioning

Set the MSBuild property `GitFlowVersioningEnabled` to `false` in your project or on the command line:

```xml
<PropertyGroup>
  <GitFlowVersioningEnabled>false</GitFlowVersioningEnabled>
</PropertyGroup>
```

## Requirements

- Git must be available on the `PATH`
- The repository must have at least one commit
- .NET 8.0 runtime or later (for CLI tool)

## Fault Tolerance

The task is designed to never fail a build. If it cannot compute a version for any reason (e.g. Git is not installed, the directory is not a repository, or the history is unreadable), it logs an MSBuild warning and substitutes a placeholder version of `0.0.0-unversioned`.

## Documentation

For complete installation instructions, usage examples, CI integration patterns, and detailed reference documentation, visit:

**https://timtek-systems.github.io/Timtek.GitFlowVersioning/**

## License

MIT License - see [LICENSE](LICENSE) for details.
