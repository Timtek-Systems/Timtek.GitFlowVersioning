# Timtek.GitFlowVersion.Tool

A command-line tool that computes GitFlow-based semantic versions from Git history and outputs them as JSON.

## Installation

Install as a local tool (recommended for team projects):

```shell
dotnet tool install Timtek.GitFlowVersion.Tool
```

Or install globally:

```shell
dotnet tool install --global Timtek.GitFlowVersion.Tool
```

## Usage

```shell
dotnet gitflowversion [path]
```

The tool outputs a JSON object containing all computed version variables:

```json
{
  "SemVer": "1.3.0-alpha.12",
  "FullSemVer": "1.3.0-alpha.12+12",
  "InformationalVersion": "1.3.0-alpha.12+12.Branch.develop.Sha.a1b2c3d...",
  ...
}
```

## Documentation

For complete installation instructions, usage examples, CI integration patterns, and detailed reference documentation, visit:

**https://timtek-systems.github.io/Timtek.GitFlowVersioning/**

## Requirements

- .NET 8.0 runtime or later
- Git must be available on the PATH
- The directory must be inside a Git repository

## License

MIT License - see [LICENSE](https://github.com/Timtek-Systems/Timtek.GitFlowVersioning/blob/main/LICENSE) for details.
