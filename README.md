# Timtek.GitFlowVersion

A minimal, opinionated GitFlow-focussed versioning utility for .NET projects that automatically computes semantic version numbers from your Git history.

## Overview

This project provides two complementary tools:

1. **Timtek.GitFlowVersion** — An MSBuild task for automatic versioning in .NET projects
2. **Timtek.GitFlowVersion.Tool** — A standalone CLI tool for computing versions from Git history

Both tools automatically compute semantic version numbers from your Git history during the build or on-demand, so you never need to maintain version numbers by hand.

## What It Does

`Timtek.GitFlowVersion` reads your branch name and the most recent version tag to produce a fully populated set of version properties including `Version`, `PackageVersion`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion`.

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
dotnet add package Timtek.GitFlowVersion
```

That's it. The next `dotnet build` or `dotnet pack` will automatically compute and apply versions. No configuration is required for standard GitFlow workflows.

### CLI Tool

Install as a local tool (recommended for team projects):

```shell
dotnet new tool-manifest
dotnet tool install Timtek.GitFlowVersion.Tool
```

Or install globally:

```shell
dotnet tool install --global Timtek.GitFlowVersion.Tool
```

In both cases, invoke the tool as `dotnet gitflowversion`.

## Usage

### MSBuild Package

Once installed, automatic versioning is enabled by default. No additional configuration is needed.

**Tagging a Release:**

Create a tag on `main` when you want to mark a release:

```shell
git tag 1.0.0
git push origin 1.0.0
```

Tags can optionally use a `v` prefix (`v1.0.0`);

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

To capture a deterministic replay fixture from a real repository:

```shell
dotnet gitflowversion --snapshot --repository /path/to/other/repo --output ./fixtures/release-1.2.3.cs
```

The generated snapshot produces a C# MSpec test fixture containing the builder steps
needed to replay the repository topology and an assertion for the expected `SemVer`,
so the scenario can be replayed in tests without depending on the original Git history.

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

Full documentation lives in the Obsidian vault at [`docs/Timtek.GitFlowVersion/`](docs/Timtek.GitFlowVersion/).

| Document | Contents |
|---|---|
| [Getting Started](docs/Timtek.GitFlowVersion/Getting%20Started.md) | Installation, first build, tagging releases |
| [CLI Tool](docs/Timtek.GitFlowVersion/CLI%20Tool.md) | Standalone version computation, JSON output, snapshot capture |
| [How It Works](docs/Timtek.GitFlowVersion/How%20It%20Works.md) | Branch classification, version computation, MSBuild integration |
| [Version Variables](docs/Timtek.GitFlowVersion/Version%20Variables.md) | Complete variable reference and MSBuild property mapping |
| [CI Integration](docs/Timtek.GitFlowVersion/CI%20Integration.md) | GitHub Actions and TeamCity setup |
| [FAQ](docs/Timtek.GitFlowVersion/FAQ.md) | Common questions and troubleshooting |

## Release Notes

### 3.2.1

- **Fix**: When a tag with prefix `v` was found, it was always used as the version root even if a nearer tag without a 'v' prefix existed.
- Versioning gap due to GitHub CI computing the wrong version.

### 3.1.0

- **Release branch tag override**: tagging a commit on a `release/*` branch with a full prerelease SemVer (e.g. `6.3.0-rc.1`) now uses that tag verbatim as the version for that exact commit — handy for promoting a `-beta` build to a `-rc` build for testers. The override applies only to that tagged commit; the next commit reverts automatically to normal `beta.N` numbering.
- **Weighted assembly versions**: `AssemblySemVer`/`AssemblySemFileVer` now derive their revision component from a branch-type weight (`develop` = 0, `release`/`hotfix`/other = 30000, stable `main` = 55000) plus the **total commit count** on `HEAD`, so builds from different branch types can never collide on the same assembly version.
- **Fix**: the weighted assembly version revision is now based on total commit count rather than distance from the nearest tag or a tag's own embedded pre-release number. Previously, force-moving a tag to a later commit (without renaming it) could leave the assembly version unchanged or even lower than a previous build, which meant tools like ClickOnce failed to detect the new build as an update. Total commit count always increases, regardless of tag movement.
- **Fix**: on `release/*` and `hotfix/*` branches, the branch-aware commit distance now only uses the merge-base distance (from `develop`/`main`) when it is *smaller* than the distance to the nearest tag, so a tag placed directly on the branch always takes precedence.
- Added a NuGet pack dependency validation script (`Validate-PackProjectReferenceVersions.ps1`) to check dependency versions in packed `.nuspec` files during CI.

### 3.0.3

- Improved SemVer tag matching so it more accurately targets version tags and is no longer distracted by CI `build-*` tags.

### 3.0.2

- Fixed several build system issues, including writing the CI build number to the MSBuild logger instead of the console, and corrected versioning import paths and file naming for consistency.

### 3.0.1

- Fixed TeamCity hotfix branch version parsing.
- Removed an experimental integration test that didn't work out.

### 1.2.0

- Excluded integration tests from CI builds (they require a `git` executable and a writable filesystem, so they now run locally only).
- Updated the GitHub Actions CI build to the latest Node.js versions.
- Corrected documentation to match actual behaviour.

### 1.0.5

- **Branch-aware versioning**: release/hotfix branches now compute commit distance using the merge-base with `develop`/`main` as appropriate, and use the branch name's version suffix as the base version when valid; exact tagged commits are treated as stable releases.
- Renamed the project from `GitFlowVersioning` to `GitFlowVersion`.
- Added the `Timtek.GitFlowVersion.IntegrationSpecs` project for end-to-end testing against real temporary Git repositories, plus a CLI snapshot capture feature (`dotnet gitflowversion snapshot`) that generates replayable C# MSpec test fixtures from real repository history.
- Refactored the CLI tool to use proper command-line options.

### 1.0.4

- Simplified documentation hosting: dropped GitHub Pages in favour of a single README.

### 1.0.3

- Improved the documentation workflow (dependencies, validation, `.gitignore`).

### 1.0.2

- Fixed the NuGet package README path so package documentation renders correctly.

### 1.0.1

- Unified and updated the README; fixed NuGet documentation packaging.

### 1.0.0

- First stable release.
- Self-versioning bootstrap: the tool now computes and applies its own version during its own CI build.
- Added a full documentation site (MkDocs, published to GitHub Pages).
- Added the MIT license and NuGet package metadata (license, README).

### 0.0.1 – 0.0.5

- Initial project scaffolding: solution structure, MSBuild task, and CLI tool.
- Iterated on NuGet package versioning and CI build configuration until packaging was reliable across multi-target builds.
