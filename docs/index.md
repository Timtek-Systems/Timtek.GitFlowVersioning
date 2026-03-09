# Timtek.GitFlowVersioning

**Automatic semantic versioning for .NET projects that follow GitFlow — zero configuration required.**

---

## Why This Exists

Most .NET versioning tools try to be everything to everyone.
They support dozens of branching strategies, hundreds of configuration options, and
require significant setup before they produce a useful version number.
For teams that have already committed to [GitFlow](https://nvie.com/posts/a-successful-git-branching-model/),
all of that flexibility is overhead.

`Timtek.GitFlowVersioning` takes the opposite approach.
It is an **opinionated** tool that delivers a **single, specific use-case** and
deliberately excludes everything else:

> **If you use GitFlow and .NET SDK-style projects, install the package and you are done.**

There is no configuration file, no YAML schema to learn, no command-line flags to remember.
The tool reads your Git history, classifies your branch, and stamps every build artefact
with the correct [SemVer 2.0](https://semver.org/) version — automatically.

## Design Philosophy

| Principle | What it means in practice |
|---|---|
| **Zero configuration** | Install the NuGet package. There is nothing else to do. |
| **Convention over configuration** | Branch names *are* the configuration. `main` is stable, `develop` is alpha, `release/*` is beta. |
| **Opinionated scope** | Only GitFlow is supported. If you use trunk-based development or another model, this is not the right tool. |
| **Never break a build** | If the tool cannot compute a version (no Git, shallow clone, missing tags), it logs a warning and falls back to `0.0.0-unversioned`. |
| **No runtime dependency** | The package is a `DevelopmentDependency`. It participates only at build time and adds nothing to your deployed application. |

## At a Glance

```shell
# Install — this is the only step
dotnet add package Timtek.GitFlowVersioning
```

From this point on, every `dotnet build` and `dotnet pack` produces correctly versioned
assemblies and NuGet packages based on your Git branch and tags.

## What Gets Set

The MSBuild task sets all of the standard .NET version properties:

- `Version` / `PackageVersion` — the SemVer string used by NuGet
- `AssemblyVersion` — the four-part assembly version
- `FileVersion` — the Win32 file version
- `InformationalVersion` — the full version with build metadata

It also generates a `GitVersionInformation` class containing every computed variable,
available for runtime introspection via the [`TA.Utils.Core`](https://www.nuget.org/packages/TA.Utils.Core) NuGet package.

## Who Is This For?

- .NET teams using **GitFlow** with `main`, `develop`, `release/*`, and `hotfix/*` branches
- Projects built with **SDK-style** `.csproj` files and the `dotnet` CLI
- CI environments including **GitHub Actions** and **TeamCity**

If your workflow matches these conventions, you will never need to think about versioning again.

!!! note "Not for every project"
    This tool deliberately does not support trunk-based development, custom branch naming,
    or version configuration files. If you need that flexibility, consider
    [GitVersion](https://gitversion.net/) or [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)
    which are excellent, broadly-scoped alternatives.
