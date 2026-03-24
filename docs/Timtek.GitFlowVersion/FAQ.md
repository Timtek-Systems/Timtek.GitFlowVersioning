# FAQ

## General

### Why not just use GitVersion or Nerdbank.GitVersioning?

Those are excellent, widely-used tools. They support many branching strategies,
extensive configuration, and a broad range of scenarios.

`Timtek.GitFlowVersion` exists because we wanted something simpler.
Our projects all use GitFlow, and we found ourselves writing the same
configuration files and debugging the same edge cases across dozens of repositories.

This tool trades flexibility for simplicity: if you use GitFlow, install the
package and you are done. No configuration files, no learning curve, no surprises.

If your workflow differs from standard GitFlow or you need per-repository configuration,
one of those tools is a better choice.

### What branching strategies are supported?

Only [GitFlow](https://nvie.com/posts/a-successful-git-branching-model/) with
the standard branch naming conventions:

- `main` (or `master`)
- `develop`
- `release/*`
- `hotfix/*`
- `feature/*`, `bugfix/*`, and any other prefix

This is a deliberate design decision, not a limitation that will be "fixed" later.

### Is there a configuration file?

No. The tool is zero-configuration by design. Branch names are the only input,
and they follow the GitFlow convention.

---

## Versioning

### Where does the base version come from?

From the most recent Git tag that matches `*.*.*` (with or without a `v` prefix),
located using `git describe --tags --long`.

If no matching tag exists, the fallback base version is `0.1.0` and the commit
distance is the total commit count in the repository.

### How does the commit distance affect the version?

It depends on the branch type:

| Branch | Effect of commit distance |
|---|---|
| `main` | Added to the patch component (`1.2.0` + 3 commits = `1.2.3`) |
| `develop` | Becomes the pre-release number (`1.3.0-alpha.5`) |
| `release/*` / `hotfix/*` | Becomes the pre-release number (`1.3.0-beta.4`), measured from the merge-base |
| Other | Becomes the pre-release number (`1.3.0-alpha.7`) |

See [[How It Works]] for a full explanation of each branch's versioning formula.

### What version is produced on `develop`?

The minor version is incremented by one from the base tag (anticipating the next
release) and the patch is reset to zero. The commit distance becomes the
`alpha` pre-release number:

```
Base tag: 1.2.0    Distance: 12    →    1.3.0-alpha.12
```

### Can I override the version?

Yes. Pass `/p:Version=X.Y.Z` on the command line:

```shell
dotnet build /p:Version=2.0.0-custom.1
```

The task will not overwrite `Version` or `PackageVersion` if they have already
been set explicitly.

---

## Build and IDE

### My IDE shows errors about `GitVersionInformation` not existing

This is expected. The `GitVersionInformation` class is generated during compilation
and does not exist in your source tree. The IDE's design-time analysis cannot see it.

Use the `GitVersion` class from
[`TA.Utils.Core`](https://www.nuget.org/packages/TA.Utils.Core) for safe runtime
access to version information.

### The version shows `0.0.0-unversioned`

This means the task could not compute a version. Common causes:

1. **Git is not on the `PATH`.**
   Verify by running `git --version` in a terminal.

2. **The directory is not a Git repository.**
   The task walks up from the project directory looking for a `.git` folder.

3. **Shallow clone in CI.**
   Use `fetch-depth: 0` in your checkout step. See [[CI Integration]] for details.

4. **No commits in the repository.**
   The task requires at least one commit.

The task logs an MSBuild warning with the specific error. Check the build output
for `Timtek.GitFlowVersioning: Failed to compute version:` messages.

### Does the task slow down my build?

The task runs a small number of `git` commands (typically 2–3) and generates two
small source files. The overhead is negligible — usually under 100ms.

### Does it work with multi-targeting projects?

Yes. The task runs during each inner (per-TFM) build and during the outer build
for NuGet pack operations. Both `build/` and `buildMultiTargeting/` targets are
included in the package.

---

## CI

### Why does CI produce a different version than my local build?

The most common cause is a **shallow clone**. CI systems often clone only the
most recent commit, which means `git describe` cannot find version tags.

Ensure your CI checkout fetches full history:

```yaml
# GitHub Actions
- uses: actions/checkout@v4
  with:
    fetch-depth: 0
```

See [[CI Integration]] for platform-specific guidance.

### Does the task detect my CI system automatically?

Yes. GitHub Actions and TeamCity are detected via their standard environment
variables (`GITHUB_ACTIONS` and `TEAMCITY_VERSION` respectively).
No CI-specific configuration is needed.

---

## See Also

- [[How It Works]] — detailed version computation rules
- [[Version Variables]] — complete variable reference
- [[CI Integration]] — CI-specific configuration and troubleshooting
