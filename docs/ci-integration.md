# CI Integration

`Timtek.GitFlowVersioning` automatically detects supported CI environments and emits
service messages so that the computed version is visible in build logs and available
to downstream steps.

No configuration is needed — detection is based on environment variables that CI
systems set automatically.

## GitHub Actions

When the `GITHUB_ACTIONS` environment variable is present, the task:

1. Emits `::notice` annotations with the computed version values, visible in the
   Actions job summary.
2. Writes `semver`, `fullSemVer`, and `informationalVersion` to the
   `$GITHUB_OUTPUT` file for use in subsequent steps.

### Using Outputs in Workflow Steps

```yaml
- name: Build
  id: build
  run: dotnet build

- name: Deploy
  run: echo "Deploying version ${{ steps.build.outputs.semver }}"
```

### Recommended Checkout Settings

The versioning task uses `git describe` to locate the nearest tag. GitHub Actions
performs a **shallow clone** by default, which may not include tag history.
Always use `fetch-depth: 0`:

```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0
```

!!! warning "Shallow clones"
    Without `fetch-depth: 0`, the task may not find version tags and will fall back
    to `0.0.0-unversioned`. This is the most common cause of unexpected versions in CI.

### Detached HEAD

GitHub Actions checks out a specific commit (detached HEAD) rather than a branch.
The task handles this by using `git name-rev` to determine the logical branch name.

## TeamCity

When the `TEAMCITY_VERSION` environment variable is present, the task emits
TeamCity service messages:

```
##teamcity[buildNumber '1.2.3-alpha.5+5']
##teamcity[setParameter name='GitFlowVersion.SemVer' value='1.2.3-alpha.5']
##teamcity[setParameter name='GitFlowVersion.FullSemVer' value='1.2.3-alpha.5+5']
##teamcity[setParameter name='GitFlowVersion.InformationalVersion' value='1.2.3-alpha.5+5.Branch.develop.Sha.abc123...']
```

This automatically:

- Sets the **build number** in TeamCity to the full semantic version.
- Exposes `GitFlowVersion.SemVer`, `GitFlowVersion.FullSemVer`, and
  `GitFlowVersion.InformationalVersion` as **build parameters** available to
  subsequent build steps and dependent builds.

### TeamCity Configuration

No special configuration is needed beyond ensuring that:

1. The build checkout includes **full history** (not a shallow clone).
2. Git is available on the agent's `PATH`.

## Other CI Systems

For CI systems that are not explicitly supported, the task still sets all MSBuild
version properties. The version is visible in the build output log and can be
captured from the `Version` or `PackageVersion` MSBuild properties.

If you need to extract the version in a script, you can read it from the built
assembly or NuGet package, or add a custom MSBuild target that writes it to a file:

```xml
<Target Name="WriteVersion" AfterTargets="_ComputeGitFlowVersion">
  <WriteLinesToFile File="$(IntermediateOutputPath)version.txt"
                    Lines="$(_GFV_SemVer)"
                    Overwrite="true" />
</Target>
```
