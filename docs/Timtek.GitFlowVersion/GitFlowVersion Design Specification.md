# A Minimal, Opinionated, GitFlow-focused Versioning Utility for .NET)

> **Document status:** Draft design spec
>
> **Audience:** .NET build/release engineers; tooling developers
>
> **Primary goal:** A *pared-down*, GitVersion-like versioning utility optimized for **GitFlow** and **MSBuild**, distributed as a **NuGet package**, working consistently in **Visual Studio**, **dotnet build**, **TeamCity (Windows/Linux)**, and **GitHub Actions**.

---

## 1. Background & Motivation

GitVersion demonstrated that Git history + branching conventions can produce deterministic, semver-compatible versions, and that those versions can be injected into builds (assemblies, packages, and generated code) with minimal build script work. GitVersion’s MSBuild approach includes generating version attributes during build (e.g., `AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion`). [1](https://gitversion.net/docs/usage/msbuild)

GitVersion also exposes a stable set of named “version variables” (e.g., `FullSemVer`, `AssemblySemVer`, `InformationalVersion`) with documented meanings and default formats. [2](https://gitversion.net/docs/reference/variables)

GitFlow-specific semantics (e.g., main releases, develop alphas, hotfix/release betas) are well described in GitVersion’s GitFlow strategy documentation, including how branches and tags are treated. [3](https://gitversion.net/docs/learn/branching-strategies/gitflow/)

This spec proposes an **in-house** tool that keeps only the capabilities needed for:

- GitFlow branch naming and behavior
- deterministic unique version per commit
- assembly + NuGet package versioning
- build log output
- injected `GitVersionInformation` class compatibility
- TeamCity build numbering

---

## 2. Scope

### 2.1 In-Scope Requirements (Must)

> AXIOM: All builds with different source inputs shall have different version strings. This requires that every commit produces a different semantic version string.

> AXIOM: A build shall never fail due to this utility. If for any reason a version string cannot be computed, an explanation shall be emitted into the logs and the version shall be `0.0.0-unversioned`.

The tool MUST:

1. Support **GitFlow branching strategy** (`main`/`master` + `develop` + `feature/*` + `release/*` + `hotfix/*`). [3](https://gitversion.net/docs/learn/branching-strategies/gitflow/)[4](https://www.datacamp.com/tutorial/gitflow)
2. Be **deployable via NuGet** package(s) and usable as a development dependency.
3. Work in:
   - **Visual Studio** builds (desktop MSBuild)
   - **dotnet build** builds (MSBuild on .NET)
   - **TeamCity** agents on Windows and Linux
   - **GitHub Actions**
4. Require **no modifications to individual `.csproj` files ideally**:
   - Preferred installation is repo-level (`Directory.Build.props` / central package management), not per-project edits.
5. Output computed version strings to the **build log** every build.
6. Version **NuGet packages** produced by `dotnet pack`.
7. Version-stamp **assemblies** (version attributes).
8. Generate an injected **static class** compatible with GitVersion:
   - same type name: `GitVersionInformation`
   - same *namespace behavior* as GitVersion default (global namespace)
   - fields as compile-time constants as GitVersion commonly emits (`public const string ...`). Example structure appears in GitVersion issue discussions around generated `GitVersionInformation.g.cs`. [5](https://github.com/GitTools/GitVersion/issues/4196)[6](https://github.com/GitTools/GitVersion/issues/4102)
9. Provide **default GitFlow branch semantics**:
   - **main/master:** stable release versions (no prerelease label)
   - **release/*** and **hotfix/*** branches: `-beta` prereleases
   - all other branches (including `develop`, `feature/*`, `bugfix/*`, etc.): `-alpha` prereleases
10. Produce a **unique version string for every commit**.

### 2.2 Non-Goals (Explicitly Out of Scope)

- Supporting multiple branching strategies beyond GitFlow (e.g., GitHubFlow, trunk-based) out of the box.
- Advanced GitVersion configuration options, plugins, or “modes” beyond what’s needed here.
- Rewriting/patching `AssemblyInfo.cs` files on disk; versions should be injected at build time.
- Full parity with GitVersion’s entire variable set beyond what is required for compatibility (but we will match the commonly-used variables list).

---

## 3. High-Level Approach

Deliver a NuGet package that:

- imports **MSBuild targets** automatically (including `buildTransitive` so repo-level install applies to all projects),
- runs a **version computation step** early in the build,
- sets MSBuild properties used for:
  - assembly stamping (version attributes)
  - packaging (`PackageVersion`)
- emits:
  - a generated `GitVersionInformation.g.cs` into `IntermediateOutputPath`
  - build log output summarizing computed versions
- optionally emits CI-friendly service messages (TeamCity, GitHub Actions) as plain log lines.

The GitVersion MSBuild approach injects version metadata into assemblies by generating a temporary assembly info file at build time. [1](https://gitversion.net/docs/usage/msbuild)

It may be worth exploring alternative approaches, as GitVersion's approach is occasionally problematic. We can ignore legacy .NET Framework builds and focus on building SDK-style projects targeting `netstandard2.0` and `.NET 8` and up. This may release us from backward-compatibility constraints imposed by .NET Framework. However, if no clearly superior approach can be found, we must adopt the same pattern as GitVersion but keep configuration and behaviour minimal.

---

## 4. Packaging & Distribution

### 4.1 NuGet Packages

**Primary package**: `Timtek.GitFlowVersion`

Contents:
- `build/Timtek.GitFlowVersion.props`
- `build/Timtek.GitFlowVersion.targets`
- `buildTransitive/Timtek.GitFlowVersion.props`
- `buildTransitive/Timtek.GitFlowVersion.targets`
- MSBuild task assemblies (multi-targeted)
- any helper executables if needed (optional; see below)

**Optional secondary package**: `Timtek.GitFlowVersion.Tool` (dotnet tool)

Not required for MSBuild integration, but can:
- print variables as JSON (GitVersion-like)
- validate repo state
- aid debugging in CI

### 4.2 Installation (No `.csproj` edits preferred)

Supported “minimal-touch” installs:

1. **Central Package Management**: add package version to `Directory.Packages.props`, and reference once in `Directory.Build.props` (repo-level).
2. **Directory.Build.props**: add a single `PackageReference` (or `PackageVersion`) that flows to all SDK-style projects via `buildTransitive`.

> Note: A *completely zero-touch* install (no repo file change at all) is not feasible with NuGet/MSBuild alone; MSBuild must import the targets somehow. The design goal is “no per-project `.csproj` edits,” not “no repo edits.”

---

## 5. Compatibility Targets

### 5.1 MSBuild Task Target Frameworks

To support **Visual Studio desktop MSBuild** and **dotnet msbuild**, the MSBuild task assemblies should multi-target:

- `net472` or `net48` (Visual Studio MSBuild typically runs on .NET Framework)
- `netstandard2.0` (consumable by dotnet MSBuild task host)
- optionally `net8.0` for modern tool host performance (not required)

This avoids the incompatibility noted in GitVersion’s newer MSBuild task docs regarding Visual Studio’s MSBuild runtime. [1](https://gitversion.net/docs/usage/msbuild)

### 5.2 Git Interaction Strategy (Cross-Platform)

To work reliably on Windows/Linux and avoid native library deployment issues, the default Git interaction SHOULD use the **`git` CLI** (invoked as a process), not libgit2 bindings.

Assumption: build agents have `git` installed (standard in TeamCity and GitHub-hosted runners).

If computing a version string is not possible for any reason, then an explanatory note shall be emitted into the build log, and the version string shall be `0.0.0-unversioned`.

---

## 6. Inputs & Configuration

### 6.1 Configuration File

There shall be no configuration file. This utility is opinionated, zero-configuration, and shall adopt Git Flow default semantics.

- `mainBranchNames`: [`main`, `master`]
- `developBranchName`: `develop`
- `tagPrefixRegex`: no prefix
- `versionTagRegex`: `^(?<prefix>[vV]?)(?<version>\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:\.[0-9A-Za-z]+)*)?)$`
- `fallbackBaseVersion`: `0.1.0`

### 6.2 MSBuild Properties (Overrides)

Allow overriding via MSBuild properties:

- `GitFlowVersioningEnabled` (default `true`)

---

## 7. Outputs (Version Variables)

The tool MUST compute and expose a set of variables aligned with GitVersion’s “Version Variables” reference. [2](https://gitversion.net/docs/reference/variables)

### 7.1 Canonical Variables (Minimum Set)

At minimum, compute:

- `Major`, `Minor`, `Patch` [2](https://gitversion.net/docs/reference/variables)
- `BranchName`, `EscapedBranchName` [2](https://gitversion.net/docs/reference/variables)
- `Sha`, `ShortSha` [2](https://gitversion.net/docs/reference/variables)
- `MajorMinorPatch` [2](https://gitversion.net/docs/reference/variables)
- `SemVer`, `FullSemVer` [2](https://gitversion.net/docs/reference/variables)
- `PreReleaseLabel`, `PreReleaseLabelWithDash` [2](https://gitversion.net/docs/reference/variables)
- `PreReleaseNumber`, `PreReleaseTag`, `PreReleaseTagWithDash` [2](https://gitversion.net/docs/reference/variables)
- `BuildMetaData`, `FullBuildMetaData` [2](https://gitversion.net/docs/reference/variables)
- `InformationalVersion` (used for `AssemblyInformationalVersion`) [2](https://gitversion.net/docs/reference/variables)
- `AssemblySemVer` and `AssemblySemFileVer` with weighted prerelease defaults:
  - `main` / `master` has branch weight `55000`, but stable builds still use `Major.Minor.Patch.0`
  - Stable builds use `Major.Minor.Patch.0`
  - `develop` uses `Major.Minor.Patch.(0 + PreReleaseNumber)`
  - `release/*`, `hotfix/*`, and other prerelease branches use `Major.Minor.Patch.(30000 + PreReleaseNumber)`

> Note: GitVersion’s variable documentation provides these defaults and intended usage. [2](https://gitversion.net/docs/reference/variables)

### 7.2 Uniqueness Rule

Commits on `main`/`master` branches are assumed to be deployable releases, therefore should not have any prerelease tag. Every commit shall increment the `Patch` version unless overridden by a tag.

For other branches, the tool MUST produce a unique version for every commit. This is achieved by:

- using a commit-distance based numeric component (`PreReleaseNumber` and/or `BuildMetaData`), and
- always including a SHA-bearing `FullBuildMetaData` component.

GitVersion’s `FullBuildMetaData` concept explicitly combines build metadata with branch and SHA. [2](https://gitversion.net/docs/reference/variables)

---

## 8. GitFlow Versioning Semantics (Default Behavior)

This section defines the default behavior required by the user and consistent with GitFlow concepts described by GitVersion. [3](https://gitversion.net/docs/learn/branching-strategies/gitflow/)[4](https://www.datacamp.com/tutorial/gitflow)

### 8.1 Branch Classification

Determine `BranchType` by case-insensitive `BranchName`:

- **Main branch**: `main` or `master`
- **Develop branch**: `develop`
- **Release branches**: `release/*`
- **Hotfix branches**: `hotfix/*`
- **Other branches**: everything else (`feature/*`, `bugfix/*`, etc.)

GitVersion’s GitFlow docs describe release and hotfix branch prefixes and that tags on main reflect stable releases. [3](https://gitversion.net/docs/learn/branching-strategies/gitflow/)

### 8.2 Version Source (Normative Precedence Rules)

Compute a “version source” commit and base version using the following strict precedence:

1. Determine the nearest reachable semantic tag from `HEAD` using `git describe --tags --long` semantics.
2. A semantic tag MAY be either stable (`x.y.z`) or prerelease (`x.y.z-label.n`).
3. The nearest semantic tag becomes the **authoritative base tag**.
4. If no semantic tag is reachable, use `fallbackBaseVersion` (`0.1.0`).

Compute:

- `VersionSourceSha`
- `CommitsSinceVersionSource`

#### 8.2.1 Tie-break/override rule for branch-aware distance

For `release/*` and `hotfix/*` branches, branch-aware merge-base distance may be used only when it is **strictly smaller** than tag-based distance. It MUST NOT override a nearer explicit tag found on the active branch.

#### 8.2.2 Tag persistence rule

Once a semantic tag is selected as base (e.g., `2.0.0-beta.12`), it remains the base for subsequent commits until a newer semantic tag is encountered.

### 8.3 Main/Master Behavior (Stable)

On `main`/`master`:

- `PreReleaseLabel` MUST be empty.
- `FullSemVer` SHOULD be `Major.Minor.Patch`.
- For non-tagged commits on `main`/`master`, the tool MUST still be unique per commit by incrementing `Patch` version (note: build metadata is not part of semantic precedence and cannot alone produce SemVer uniqueness).
- `InformationalVersion` should follow the GitVersion definition: defaults to `FullSemVer` suffixed by `FullBuildMetaData`. [2](https://gitversion.net/docs/reference/variables)

### 8.4 Release/* and Hotfix/* Behavior (Beta)

On `release/*` and `hotfix/*`:

- `PreReleaseLabel` MUST be `beta`.
- `SemVer` MUST include `-beta.{n}`.
- `FullSemVer` MUST include prerelease and may include build metadata.

`PreReleaseNumber` MUST be computed as follows:

1. If `HEAD` is exactly tagged with `x.y.z-beta.k`, then `PreReleaseNumber = k`.
2. Else if authoritative base tag is `x.y.z-beta.k`, then `PreReleaseNumber = k + CommitsSinceVersionSource`.
3. Else `PreReleaseNumber = CommitsSinceVersionSource`.

This makes `2.0.0-beta.12-1-g<sha>` produce `2.0.0-beta.13`, and guarantees monotonic prerelease progression on release/hotfix branches.

### 8.5 All Other Branches (Alpha)

On `develop` and all other non-main branches:

- `PreReleaseLabel` MUST be `alpha`.
- `PreReleaseNumber` MUST ensure uniqueness (commit-distance based by default).

GitVersion’s GitFlow doc describes `develop` as `alpha.{n}` and minor bump relative to main. [3](https://gitversion.net/docs/learn/branching-strategies/gitflow/)

- This tool MAY adopt the same “develop is next minor” rule, but since requirements only mandate alpha/beta labeling, the “next minor” strategy is configurable.
- Default behavior SHOULD follow GitVersion’s GitFlow approach:
  - Base version from main
  - `develop` uses `Minor = mainMinor + 1`, `Patch = 0`, prerelease `alpha.{n}` [3](https://gitversion.net/docs/learn/branching-strategies/gitflow/)

### 8.6 Executable Acceptance Examples (Normative)

The following examples are required acceptance contracts:

1. `Branch=release/2.0.0`, `TagAtHead=2.0.0-beta.12`, `Distance=0` => `SemVer=2.0.0-beta.12`
2. `Branch=release/2.0.0`, `NearestTag=2.0.0-beta.12`, `Distance=1` => `SemVer=2.0.0-beta.13`
3. `Branch=release/2.0.0`, `NearestTag=2.0.0-beta.12`, `Distance=5` => `SemVer=2.0.0-beta.17`
4. `Branch=hotfix/3.0.1`, `NearestTag=3.0.1-beta.7`, `Distance=3` => `SemVer=3.0.1-beta.10`
5. `Branch=feature/foo`, `NearestTag=2.0.0-beta.4`, `Distance=8` => `SemVer=2.0.0-alpha.8`

---

## 9. Build Integration (MSBuild)

### 9.1 Targets & Execution Points

The `.targets` file MUST:

1. Run version calculation **before** compilation and packing:
   - before `CoreCompile`
   - before `GenerateAssemblyInfo` (or override it)
   - before `Pack`
2. Generate:
   - `GitVersionInformation.g.cs` in `$(IntermediateOutputPath)`
   - `Timtek.GitFlowVersion.AssemblyInfo.g.cs` (or set equivalent MSBuild properties)

GitVersion’s MSBuild task generates a temporary `AssemblyInfo.cs` at build time containing the appropriate version attributes. [1](https://gitversion.net/docs/usage/msbuild)

### 9.2 Assembly Stamping

The tool MUST stamp:

- `AssemblyVersion` <- `AssemblySemVer` [1](https://gitversion.net/docs/usage/msbuild)[2](https://gitversion.net/docs/reference/variables)
- `AssemblyFileVersion` <- `AssemblySemFileVer` [2](https://gitversion.net/docs/reference/variables)
- `AssemblyInformationalVersion` <- `InformationalVersion` [1](https://gitversion.net/docs/usage/msbuild)[2](https://gitversion.net/docs/reference/variables)

Implementation options:

- generate an assembly info source file with `[assembly: ...]` attributes
- or set MSBuild properties (`AssemblyVersion`, `FileVersion`, `InformationalVersion`) and rely on SDK targets

Because the requirement states “ideally no csproj modifications,” the tool SHOULD prefer the property route when possible, and fall back to generated source when SDK behaviors differ.

### 9.3 NuGet Package Versioning

For `dotnet pack`:

- set `PackageVersion` to a SemVer2 string derived from:
  - stable main: `Major.Minor.Patch` for tagged commits; otherwise a unique semver2 string
  - beta/alpha: `Major.Minor.Patch-{label}.{n}`
- set `Version` (optional) consistently with `PackageVersion`

> GitVersion’s documentation emphasizes that version variables are meant to be used for versioning assemblies and packages. [7](https://gitversion.net/docs/)[2](https://gitversion.net/docs/reference/variables)

### 9.4 Build Log Output

On every build, log a concise summary:

- `FullSemVer`
- `SemVer`
- `AssemblySemVer`
- `AssemblySemFileVer`
- `InformationalVersion`
- `BranchName`
- `Sha`

This must be visible in:

- Visual Studio Output window
- `dotnet build` console output
- TeamCity and GitHub Actions logs

---

## 10. CI Behavior

### 10.1 TeamCity

At minimum:

- log version summary lines (TeamCity will capture them)
- emit TeamCity service messages to set build number

GitVersion is commonly used to expose version variables to build servers like TeamCity. [7](https://gitversion.net/docs/)[8](https://www.dennisdoomen.com/2022/02/gitversion.html)

### 10.2 GitHub Actions

At minimum:

- log version summary lines
- emit workflow commands in logs (design choice):
  - `::notice title=Version::FullSemVer=...`
  - `echo "version=..." >> $GITHUB_OUTPUT` (only if running in a step that expects it)

### 10.3 CI Diagnostic Contract (Required for supportability)

When running in CI, logs MUST include enough information to diagnose tag-related mismatches:

- `git rev-parse HEAD`
- resolved `BranchName`
- resolved base tag
- tag distance
- whether `HEAD` is exactly tagged

For TeamCity support, this contract allows immediate differentiation between:

- exact-tag build (`2.0.0-beta.12-0`)
- one-commit-after-tag build (`2.0.0-beta.12-1`)
- missing-tag checkout problems

---

## 11. Generated `GitVersionInformation` Static Class

### 11.1 File Name & Location

Generate `GitVersionInformation.g.cs` under `$(IntermediateOutputPath)` and include it in compilation as `Compile` item.

### 11.2 Namespace Rules (Compatibility)

- generate **no namespace** (global namespace), so consuming code can reference `GitVersionInformation.FullSemVer`

GitVersion issue reports show a generated file pattern with `static class GitVersionInformation` and const string fields under a chosen namespace, including an option named similarly to `UseProjectNamespaceForGitVersionInformation`. [5](https://github.com/GitTools/GitVersion/issues/4196) We will always use the root namespace.

### 11.3 Field Set

The class MUST contain constant fields matching the chosen variable set (§7.1), using names identical to GitVersion variable names.

GitVersion’s “Version Variables” page lists the canonical variable names and meanings (e.g., `FullSemVer`, `InformationalVersion`, `AssemblySemVer`). [2](https://gitversion.net/docs/reference/variables)

**Example shape (illustrative only; values are placeholders):**

```csharp
// <auto-generated />
using System.Runtime.CompilerServices;

[CompilerGenerated]
internal static class GitVersionInformation
{
    public const string Major = "1";
    public const string Minor = "2";
    public const string Patch = "3";
    public const string PreReleaseLabel = "alpha";
    public const string PreReleaseLabelWithDash = "-alpha";
    public const string PreReleaseNumber = "42";
    public const string PreReleaseTag = "alpha.42";
    public const string PreReleaseTagWithDash = "-alpha.42";
    public const string BranchName = "feature/foo";
    public const string EscapedBranchName = "feature-foo";
    public const string Sha = "0123456789abcdef...";
    public const string ShortSha = "0123456";
    public const string MajorMinorPatch = "1.2.3";
    public const string SemVer = "1.2.3-alpha.42";
    public const string FullSemVer = "1.2.3-alpha.42+99";
    public const string BuildMetaData = "99";
    public const string FullBuildMetaData = "99.Branch.feature/foo.Sha.0123456789abcdef...";
    public const string InformationalVersion = "1.2.3-alpha.42+99.Branch.feature/foo.Sha.012345...";
    public const string AssemblySemVer = "1.2.3.30042";
    public const string AssemblySemFileVer = "1.2.3.30042";
}


