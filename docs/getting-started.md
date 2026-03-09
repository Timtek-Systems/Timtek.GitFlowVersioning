# Getting Started

## Installation

Add the package to your project:

```shell
dotnet add package Timtek.GitFlowVersioning
```

That is the only step. There are no configuration files to create, no properties to set,
and no command-line flags to learn.

!!! success "Zero configuration"
    The package is deliberately designed so that installation is the *entire* setup process.
    If you follow standard GitFlow conventions, everything works out of the box.

## What Happens Next

On the next `dotnet build`, the MSBuild task automatically:

1. Detects the current Git branch and the most recent version tag.
2. Computes a [SemVer 2.0](https://semver.org/) version based on the branch type and commit distance.
3. Sets `Version`, `PackageVersion`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion`.
4. Generates a `GitVersionInformation.g.cs` source file in the intermediate output directory.

You can verify that versioning is active by checking the build output:

```
Timtek.GitFlowVersioning:
  FullSemVer:           1.2.3-alpha.5+5
  SemVer:               1.2.3-alpha.5
  AssemblySemVer:       1.2.0.0
  AssemblySemFileVer:   1.2.3.0
  InformationalVersion: 1.2.3-alpha.5+5.Branch.develop.Sha.a1b2c3d...
  BranchName:           develop
  Sha:                  a1b2c3d4e5f6...
```

## Tagging a Release

Version numbers are derived from Git tags. Create a tag on `main` when you are ready
to mark a release:

```shell
git tag 1.0.0
git push origin 1.0.0
```

Tags can use a `v` prefix (`v1.0.0`) — both formats are recognised.

!!! tip "Tag placement matters"
    Tags should be placed on `main` (or `master`). The commit distance from the nearest
    tag drives the patch number on `main` and the pre-release number on other branches.

## Packaging

NuGet packages are versioned automatically:

```shell
dotnet pack
```

The resulting `.nupkg` will carry the computed `PackageVersion` without any manual
`/p:Version=` overrides needed.

## Requirements

- **Git** must be available on the system `PATH`.
- The repository must have **at least one commit**.
- Projects must use **SDK-style** `.csproj` files (the modern format used by `dotnet new`).

## Removing or Disabling

To temporarily disable versioning without removing the package, set the MSBuild property:

=== "Project file"

    ```xml
    <PropertyGroup>
      <GitFlowVersioningEnabled>false</GitFlowVersioningEnabled>
    </PropertyGroup>
    ```

=== "Command line"

    ```shell
    dotnet build /p:GitFlowVersioningEnabled=false
    ```

To remove the package entirely:

```shell
dotnet remove package Timtek.GitFlowVersioning
```
