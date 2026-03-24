# How It Works

## Overview

`Timtek.GitFlowVersion` ships as a NuGet package containing an MSBuild task.
The task runs automatically before compilation and performs four steps:

1. **Inspect** — reads the current branch name and runs `git describe` to find the nearest version tag.
2. **Classify** — maps the branch name to a GitFlow category.
3. **Compute** — calculates the full set of version variables from the branch type, base tag, and commit distance.
4. **Apply** — sets MSBuild properties and generates source files so the compiler, assembly metadata, and NuGet pack all receive the correct version.

The entire process is transparent. There are no configuration files and no decisions for the developer to make.

## Branch Classification

The tool recognises five branch categories:

| Branch pattern | Classification | Pre-release label |
|---|---|---|
| `main` or `master` | Main | *(none — stable release)* |
| `develop` | Develop | `alpha` |
| `release/*` | Release | `beta` |
| `hotfix/*` | Hotfix | `beta` |
| Everything else (`feature/*`, `bugfix/*`, etc.) | Other | `alpha` |

Branch matching is **case-insensitive**. A branch named `Release/2.0` is treated
identically to `release/2.0`.

## Version Computation

### Base Version

The base version is the most recent Git tag that matches the pattern `*.*.*`
(with or without a leading `v`/`V` prefix). The `git describe --tags --long` command
is used to locate the tag and measure the commit distance from it.

If no matching tag exists in the repository, the fallback base version is `0.1.0`
and the commit distance is the total commit count.

### Main Branch

On `main`, the version is a **stable release** with no pre-release label.
The commit distance is added to the patch component of the base version:

```
Base tag: 1.2.0    Distance: 3    →    Version: 1.2.3
```

| Variable | Value |
|---|---|
| `SemVer` | `1.2.3` |
| `AssemblySemVer` | `1.2.0.0` |
| `AssemblySemFileVer` | `1.2.3.0` |
| `InformationalVersion` | `1.2.3+3.Branch.main.Sha.abc123...` |

### Develop Branch

On `develop`, the version is an **alpha pre-release**. The base version's
minor component is incremented by one (anticipating the next release), the patch
is reset to zero, and the commit distance becomes the pre-release number:

```
Base tag: 1.2.0    Distance: 12    →    Version: 1.3.0-alpha.12
```

| Variable | Value |
|---|---|
| `SemVer` | `1.3.0-alpha.12` |
| `FullSemVer` | `1.3.0-alpha.12+12` |
| `PreReleaseLabel` | `alpha` |
| `PreReleaseNumber` | `12` |

### Release and Hotfix Branches

`release/*` and `hotfix/*` branches produce **beta pre-releases**.

**Base version:** If the branch suffix is a valid SemVer (e.g., `release/1.3.0`), that
version is used directly as the base. Otherwise the most recent tag is used as a fallback.

**Commit distance:** Rather than counting commits from the nearest tag, distance is
measured from the **merge-base** with the parent branch — `develop` for `release/*`
branches and `main`/`master` for `hotfix/*` branches. This counts only the commits
that belong to the branch itself. If the merge-base cannot be determined, tag distance
is used as a fallback.

```
Branch: release/1.3.0    Distance from develop merge-base: 4    →    Version: 1.3.0-beta.4
Branch: hotfix/1.2.4     Distance from main merge-base: 1        →    Version: 1.2.4-beta.1
```

### Feature and Other Branches

Any branch that does not match the patterns above is treated as an **alpha pre-release**.
The base version is taken from the most recent tag (no minor increment) and the
commit distance from that tag becomes the pre-release number:

```
Base tag: 1.3.0    Distance: 7    →    Version: 1.3.0-alpha.7
```

### Exact Tagged Commits

When the current commit is exactly on a version tag (commit distance is zero) and the
branch is not one of the standard GitFlow branches, the commit is treated as a **stable
release** regardless of branch name. This covers detached HEAD states such as those
used by CI systems when checking out a tagged release:

```
Branch: tags/1.2.3    Tag: 1.2.3    Distance: 0    →    Version: 1.2.3
```

## Tag Format

Tags can use either bare versions or a `v` prefix:

- `1.0.0` ✓
- `v1.0.0` ✓
- `V2.1.0` ✓

The leading `v`/`V` is stripped during parsing. Three-part `Major.Minor.Patch` is expected.

## MSBuild Integration

The package includes `.props` and `.targets` files in the standard NuGet layout:

```
build/
  Timtek.GitFlowVersioning.props      ← disables SDK assembly info generation
  Timtek.GitFlowVersioning.targets    ← defines the versioning target
buildTransitive/
  ...                                 ← same files for transitive consumers
buildMultiTargeting/
  ...                                 ← outer-build support for multi-TFM projects
tasks/
  netstandard2.0/
    Timtek.GitFlowVersioning.dll      ← the MSBuild task assembly
```

The `_ComputeGitFlowVersion` target runs `BeforeTargets="CoreCompile;GenerateAssemblyInfo;BeforeBuild"`,
ensuring version properties are set before any compilation occurs.

### Design-Time Builds

The target is automatically skipped during design-time builds
(`$(DesignTimeBuild) == 'true'`) to avoid interfering with IDE responsiveness.

## Generated Source Files

Two files are generated in the intermediate output directory (`obj/`):

### `GitVersionInformation.g.cs`

An `internal static` class containing `const string` fields for every version variable.
This class is compiled into your assembly but is **not visible in your source tree** —
it exists only in the intermediate output.

> [!warning] IDE may show errors
> Because `GitVersionInformation` does not exist until compilation, the IDE may
> report errors if you reference it directly. Use the `GitVersion` class from
> [`TA.Utils.Core`](https://www.nuget.org/packages/TA.Utils.Core) for safe runtime access.

### `Timtek.GitFlowVersioning.AssemblyInfo.g.cs`

Contains `[assembly: AssemblyVersion]`, `[assembly: AssemblyFileVersion]`,
and `[assembly: AssemblyInformationalVersion]` attributes. The SDK's own assembly
info generation is disabled by the `.props` file to avoid conflicts.

## Fault Tolerance

The task is designed to **never fail a build**. If it cannot compute a version for
any reason — Git is not installed, the directory is not a repository, the history is
unreadable, or any other unexpected error occurs — it:

1. Logs an MSBuild **warning** (not an error).
2. Substitutes the placeholder version `0.0.0-unversioned`.
3. Returns `true` from the task so the build continues.

This ensures that versioning issues surface as visible warnings without blocking
development or CI pipelines.

## See Also

- [[Version Variables]] — complete reference for all computed variables and their MSBuild property names
- [[CI Integration]] — how CI-specific service messages are emitted
- [[FAQ]] — common questions about versioning behaviour
