# CLI Tool

`Timtek.GitFlowVersion.Tool` is a standalone command-line tool that computes
GitFlow-based semantic versions from Git history and outputs them as JSON.
It uses the same versioning logic as the MSBuild task but can be run independently
of a build — making it useful for CI scripting, diagnostics, automation, and
capturing deterministic test fixtures from real repositories.

## Installation

Install the tool as a **local dotnet tool** so it is available to anyone who
clones the repository:

```shell
dotnet new tool-manifest   # only needed once per repository
dotnet tool install Timtek.GitFlowVersion.Tool
```

This adds the tool to `.config/dotnet-tools.json`, which should be committed
to source control. Other developers (and CI agents) can then restore it with:

```shell
dotnet tool restore
```

You can also install it globally:

```shell
dotnet tool install --global Timtek.GitFlowVersion.Tool
```

In either case, invoke it as `dotnet gitflowversion`.

## Usage

Run the tool from any directory inside a Git repository:

```shell
dotnet gitflowversion
```

By default it uses the current working directory. You can pass an explicit path:

```shell
dotnet gitflowversion /path/to/repo
```

## Snapshot Capture

Capture a repository's structure as a replayable integration test fixture:

```shell
dotnet gitflowversion snapshot
```

Capture another repository and write the fixture to a file:

```shell
dotnet gitflowversion snapshot /path/to/other/repo ./fixtures/release-1.2.3.json
```

The snapshot analyzes the repository topology and outputs C# code that can be
pasted directly into an MSpec integration test. The generated code uses the
`VersionComputationBuilder` fluent API to reconstruct a temporary Git repository
with the same branching, tagging, and commit structure:

```csharp
// Captured scenario: release-1.0.1-1.0.1-beta.9
// Branch: release/1.0.1
// Expected SemVer: 1.0.1-beta.9

Establish context = () => Context = Builder
    .WithInitialCommit()
    .WithTag("0.2.0")
    .WithBranch("develop")
    .WithBranch("release/1.0.1")
    .WithCommits(9)
    .Build();

It should_produce_expected_semver = () => Context.Result.SemVer.ShouldEqual("1.0.1-beta.9");
```

This makes it possible to capture real-world repository states once and replay
them later in automated tests by reconstructing the repository structure on disk.

## Output

The default command prints a JSON object containing every computed version variable:

```json
{
  "Major": "1",
  "Minor": "3",
  "Patch": "0",
  "MajorMinorPatch": "1.3.0",
  "PreReleaseLabel": "alpha",
  "PreReleaseLabelWithDash": "-alpha",
  "PreReleaseNumber": "12",
  "PreReleaseTag": "alpha.12",
  "PreReleaseTagWithDash": "-alpha.12",
  "SemVer": "1.3.0-alpha.12",
  "FullSemVer": "1.3.0-alpha.12+12",
  "BranchName": "develop",
  "EscapedBranchName": "develop",
  "Sha": "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
  "ShortSha": "a1b2c3d",
  "BuildMetaData": "12",
  "FullBuildMetaData": "12.Branch.develop.Sha.a1b2c3d4e5f6...",
  "InformationalVersion": "1.3.0-alpha.12+12.Branch.develop.Sha.a1b2c3d4e5f6...",
  "AssemblySemVer": "1.3.0.0",
  "AssemblySemFileVer": "1.3.0.0"
}
```

The variables are identical to those produced by the MSBuild task.
See the [Version Variables](version-variables.md) page for a complete reference.

## Extracting Values in Scripts

The JSON output is designed to be parsed with standard tools like `jq`:

=== "Bash"

    ```bash
    SEMVER=$(dotnet gitflowversion | jq -r '.SemVer')
    echo "Version is $SEMVER"
    ```

=== "PowerShell"

    ```powershell
    $version = dotnet gitflowversion | ConvertFrom-Json
    Write-Host "Version is $($version.SemVer)"
    ```

## CI Usage

The tool is particularly useful in CI pipelines where you need the version
**before** or **independently of** the MSBuild build. A typical GitHub Actions
pattern:

```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0

- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: 9.x

- name: Restore tools
  run: dotnet tool restore

- name: Compute version
  id: version
  run: |
    VERSION_JSON=$(dotnet gitflowversion)
    SEMVER=$(echo "$VERSION_JSON" | jq -r '.SemVer')
    echo "semver=$SEMVER" >> "$GITHUB_OUTPUT"
    echo "::notice title=GitFlowVersion::SemVer=$SEMVER"

- name: Build
  run: dotnet build --configuration Release

- name: Pack
  run: >
    dotnet pack --configuration Release --output nupkgs
    -p:PackageVersion=${{ steps.version.outputs.semver }}
```

!!! tip "Bootstrapping"
    The CLI tool solves a bootstrapping problem: a repository that *produces*
    the versioning MSBuild task cannot consume its own package to version itself.
    The tool provides the same version computation without depending on the
    MSBuild integration.

## Migration Note

Earlier builds exposed the command as `gitflowversion`. After upgrading to a
package built with the `dotnet-gitflowversion` command name, reinstall the tool
and use `dotnet gitflowversion` instead.

## Exit Codes

| Code | Meaning |
|------|---------|
| `0`  | Success — version JSON or snapshot fixture was written. |
| `1`  | Failure — an error message was written to stderr. |

Unlike the MSBuild task (which never fails a build), the CLI tool returns a
non-zero exit code on error so that CI scripts can detect and handle failures.

## Requirements

- .NET 8.0 runtime or later.
- Git must be available on the `PATH`.
- The target directory must be inside a Git repository with at least one commit.
