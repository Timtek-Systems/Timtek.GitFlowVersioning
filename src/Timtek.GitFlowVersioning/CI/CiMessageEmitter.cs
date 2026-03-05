using System;
using System.IO;
using Timtek.GitFlowVersioning.Versioning;

namespace Timtek.GitFlowVersioning.CI;

/// <summary>Emits CI-specific service messages for TeamCity and GitHub Actions.</summary>
public static class CiMessageEmitter
{
    /// <summary>Emits CI service messages for the computed <paramref name="versionInfo"/> to stdout.</summary>
    /// <param name="versionInfo">The computed version information.</param>
    public static void Emit(VersionInfo versionInfo)
    {
        if (IsTeamCity())
            EmitTeamCityMessages(versionInfo);

        if (IsGitHubActions())
            EmitGitHubActionsMessages(versionInfo);
    }

    private static bool IsTeamCity() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));

    private static bool IsGitHubActions() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

    private static void EmitTeamCityMessages(VersionInfo versionInfo)
    {
        Console.WriteLine($"##teamcity[buildNumber '{versionInfo.FullSemVer}']");
        Console.WriteLine($"##teamcity[setParameter name='GitFlowVersion.SemVer' value='{versionInfo.SemVer}']");
        Console.WriteLine($"##teamcity[setParameter name='GitFlowVersion.FullSemVer' value='{versionInfo.FullSemVer}']");
        Console.WriteLine($"##teamcity[setParameter name='GitFlowVersion.InformationalVersion' value='{versionInfo.InformationalVersion}']");
    }

    private static void EmitGitHubActionsMessages(VersionInfo versionInfo)
    {
        Console.WriteLine($"::notice title=GitFlowVersion::FullSemVer={versionInfo.FullSemVer}");
        Console.WriteLine($"::notice title=GitFlowVersion::SemVer={versionInfo.SemVer}");
        Console.WriteLine($"::notice title=GitFlowVersion::InformationalVersion={versionInfo.InformationalVersion}");

        var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        if (!string.IsNullOrEmpty(githubOutput))
        {
            File.AppendAllText(githubOutput, $"semver={versionInfo.SemVer}{Environment.NewLine}");
            File.AppendAllText(githubOutput, $"fullSemVer={versionInfo.FullSemVer}{Environment.NewLine}");
            File.AppendAllText(githubOutput, $"informationalVersion={versionInfo.InformationalVersion}{Environment.NewLine}");
        }
    }
}
