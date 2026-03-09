# CLI Tool

`Timtek.GitFlowVersion.Tool` is a standalone command-line tool that computes
GitFlow-based semantic versions from Git history and outputs them as JSON.
It uses the same versioning logic as the MSBuild task but can be run independently
of a build — making it useful for CI scripting, diagnostics, and automation.

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

## Usage

Run the tool from any directory inside a Git repository:

```shell
dotnet gitflowversion
```

By default it uses the current working directory. You can pass an explicit path:

```shell
dotnet gitflowversion /path/to/repo
```

## Output

The tool prints a JSON object containing every computed version variable:

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

## Exit Codes

| Code | Meaning |
|------|---------|
| `0`  | Success — version JSON was written to stdout. |
| `1`  | Failure — an error message was written to stderr. |

Unlike the MSBuild task (which never fails a build), the CLI tool returns a
non-zero exit code on error so that CI scripts can detect and handle failures.

## Requirements

- .NET 8.0 runtime or later.
- Git must be available on the `PATH`.
- The target directory must be inside a Git repository with at least one commit.
