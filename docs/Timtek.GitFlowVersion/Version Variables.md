# Version Variables

`Timtek.GitFlowVersion` computes a comprehensive set of version variables on every build.
These are exposed in three ways:

1. **MSBuild properties** — available during the build for use in targets, conditions, and pack metadata.
2. **Generated `GitVersionInformation` class** — compiled into your assembly as `internal static` constants.
3. **Standard .NET assembly attributes** — `AssemblyVersion`, `AssemblyFileVersion`, and `AssemblyInformationalVersion`.

## Numbered Versioning Scenarios (`SemVer`)

The table below lists the expected `SemVer` output for all supported branch modes and edge cases.

| Scenario | Branch / Context | Base Version Source | Distance Source | Example Inputs | Expected `SemVer` |
|---:|---|---|---|---|---|
| 1 | Exact tagged commit (`distance = 0`) on `main` | Tag | Tag distance (`0`) | Tag: `1.2.3`, Branch: `main` | `1.2.3` |
| 2 | Exact tagged commit in detached/tag context | Tag | Tag distance (`0`) | Tag: `1.0.1`, Branch: `tags/1.0.1` | `1.0.1` |
| 3 | `main` ahead of latest tag | Tag | Tag distance | Tag: `1.2.3`, Distance: `5` | `1.2.8` |
| 4 | `develop` | Tag (minor + 1, patch reset to `0`) | Tag distance | Tag: `1.2.3`, Distance: `7` | `1.3.0-alpha.7` |
| 5 | `release/<semver>` | Branch suffix (`<semver>`) | Commits since merge-base with `develop` | Branch: `release/1.3.0`, Distance: `3` | `1.3.0-beta.3` |
| 6 | `release/<non-semver>` | Tag fallback | Commits since merge-base with `develop` | Branch: `release/candidate`, Tag: `1.2.3`, Distance: `2` | `1.2.3-beta.2` |
| 7 | `hotfix/<semver>` | Branch suffix (`<semver>`) | Commits since merge-base with `main`/`master` | Branch: `hotfix/1.2.4`, Distance: `1` | `1.2.4-beta.1` |
| 8 | `hotfix/<non-semver>` | Tag fallback | Commits since merge-base with `main`/`master` | Branch: `hotfix/fix-critical`, Tag: `1.2.3`, Distance: `2` | `1.2.3-beta.2` |
| 9 | Other branches (`feature/*`, `bugfix/*`, etc.) | Tag | Tag distance | Branch: `feature/new-ui`, Tag: `1.2.3`, Distance: `10` | `1.2.3-alpha.10` |
| 10 | No matching version tags on `main` | Fallback `0.1.0` | Repository commit count fallback | Branch: `main`, Count: `4` | `0.1.4` |
| 11 | No matching version tags on `develop` | Fallback `0.1.0` (then minor + 1) | Repository commit count fallback | Branch: `develop`, Count: `4` | `0.2.0-alpha.4` |
| 12 | No matching version tags on other branches | Fallback `0.1.0` | Repository commit count fallback | Branch: `feature/test`, Count: `4` | `0.1.0-alpha.4` |

## Complete Variable Reference

The following example assumes a repository on the `develop` branch with the most recent
tag `1.2.0` and 5 commits since that tag, at commit `a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2`.

| Variable | MSBuild Property | Example Value | Description |
|---|---|---|---|
| `Major` | `_GFV_Major` | `1` | Major version number |
| `Minor` | `_GFV_Minor` | `3` | Minor version number |
| `Patch` | `_GFV_Patch` | `0` | Patch version number |
| `MajorMinorPatch` | `_GFV_MajorMinorPatch` | `1.3.0` | Three-part version string |
| `PreReleaseLabel` | `_GFV_PreReleaseLabel` | `alpha` | Pre-release label (`alpha`, `beta`, or empty) |
| `PreReleaseLabelWithDash` | `_GFV_PreReleaseLabelWithDash` | `-alpha` | Pre-release label with leading dash |
| `PreReleaseNumber` | `_GFV_PreReleaseNumber` | `5` | Commit distance used as pre-release number |
| `PreReleaseTag` | `_GFV_PreReleaseTag` | `alpha.5` | Full pre-release tag |
| `PreReleaseTagWithDash` | `_GFV_PreReleaseTagWithDash` | `-alpha.5` | Full pre-release tag with leading dash |
| `SemVer` | `_GFV_SemVer` | `1.3.0-alpha.5` | SemVer 2.0 version string |
| `FullSemVer` | `_GFV_FullSemVer` | `1.3.0-alpha.5+5` | SemVer with build metadata |
| `BranchName` | `_GFV_BranchName` | `develop` | Current Git branch name |
| `EscapedBranchName` | `_GFV_EscapedBranchName` | `develop` | Branch name with `/` replaced by `-` |
| `Sha` | `_GFV_Sha` | `a1b2c3d4e5f6...` | Full commit SHA |
| `ShortSha` | `_GFV_ShortSha` | `a1b2c3d` | First 7 characters of the commit SHA |
| `BuildMetaData` | `_GFV_BuildMetaData` | `5` | Commit distance as build metadata |
| `FullBuildMetaData` | `_GFV_FullBuildMetaData` | `5.Branch.develop.Sha.a1b2c3d...` | Full build metadata string |
| `InformationalVersion` | `_GFV_InformationalVersion` | `1.3.0-alpha.5+5.Branch.develop.Sha.a1b2c3d...` | Complete informational version |
| `AssemblySemVer` | `_GFV_AssemblySemVer` | `1.3.0.5` | Four-part assembly version using a weighted prerelease revision |
| `AssemblySemFileVer` | `_GFV_AssemblySemFileVer` | `1.3.0.5` | Four-part file version using a weighted prerelease revision |

## Standard MSBuild Properties

In addition to the `_GFV_*` properties, the task sets the following standard MSBuild properties
that are consumed by the compiler, NuGet pack, and assembly info generation:

| MSBuild Property | Source Variable | Purpose |
|---|---|---|
| `Version` | `SemVer` | Used by NuGet for package version |
| `PackageVersion` | `SemVer` | Explicit NuGet package version |
| `AssemblyVersion` | `AssemblySemVer` | Stamped into `[AssemblyVersion]` |
| `FileVersion` | `AssemblySemFileVer` | Stamped into `[AssemblyFileVersion]` |
| `InformationalVersion` | `InformationalVersion` | Stamped into `[AssemblyInformationalVersion]` |

> [!note] Explicit version override
> If you pass `/p:Version=X.Y.Z` on the command line (for example in a CI script),
> the task will **not** override `Version` or `PackageVersion`. This allows CI
> pipelines to pin a specific version when needed.

## Using Variables in MSBuild

The `_GFV_*` properties are available after the `_ComputeGitFlowVersion` target runs.
You can reference them in your project file for custom targets:

```xml
<Target Name="PrintVersion" AfterTargets="_ComputeGitFlowVersion">
  <Message Importance="High" Text="Building version $(_GFV_FullSemVer)" />
</Target>
```

## Runtime Access

The generated `GitVersionInformation` class is `internal` and does not exist until
compilation, so direct source references will produce IDE errors.

Use the `GitVersion` class from the
[`TA.Utils.Core`](https://www.nuget.org/packages/TA.Utils.Core) NuGet package
for safe, typed access at runtime:

```csharp
var version = GitVersion.GitInformationalVersion;
Console.WriteLine(version);  // "1.2.3+5.Branch.main.Sha.a1b2c3d..."
```

## See Also

- [[How It Works]] — explains how each variable is derived from Git history
- [[FAQ]] — common questions about version number behaviour
