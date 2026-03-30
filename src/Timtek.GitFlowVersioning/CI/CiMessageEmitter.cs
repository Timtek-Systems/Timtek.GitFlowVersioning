using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.CI;

/// <summary>Emits CI-specific service messages for TeamCity and GitHub Actions.</summary>
public static class CiMessageEmitter
{
    /// <summary>Emits CI service messages for the computed <paramref name="versionInfo"/>.</summary>
    /// <param name="versionInfo">The computed version information.</param>
    /// <param name="writeLine">
    /// A delegate that writes a single line to the build output.
    /// In MSBuild tasks, pass <c>Log.LogMessage(MessageImportance.High, ...)</c>;
    /// in console tools, pass <c>Console.WriteLine</c>.
    /// </param>
    public static void Emit(VersionInfo versionInfo, Action<string> writeLine)
    {
        if (IsTeamCity())
            EmitTeamCityMessages(versionInfo, writeLine);

        if (IsGitHubActions())
            EmitGitHubActionsMessages(versionInfo, writeLine);
    }

    private static bool IsTeamCity() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));

    private static bool IsGitHubActions() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

    private static void EmitTeamCityMessages(VersionInfo versionInfo, Action<string> writeLine)
    {
        writeLine($"##teamcity[buildNumber '{versionInfo.FullSemVer}']");
        writeLine($"##teamcity[setParameter name='GitFlowVersion.SemVer' value='{versionInfo.SemVer}']");
        writeLine($"##teamcity[setParameter name='GitFlowVersion.FullSemVer' value='{versionInfo.FullSemVer}']");
        writeLine($"##teamcity[setParameter name='GitFlowVersion.InformationalVersion' value='{versionInfo.InformationalVersion}']");
    }

    private static void EmitGitHubActionsMessages(VersionInfo versionInfo, Action<string> writeLine)
    {
        writeLine($"::notice title=GitFlowVersion::FullSemVer={versionInfo.FullSemVer}");
        writeLine($"::notice title=GitFlowVersion::SemVer={versionInfo.SemVer}");
        writeLine($"::notice title=GitFlowVersion::InformationalVersion={versionInfo.InformationalVersion}");

        var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        if (!string.IsNullOrEmpty(githubOutput))
        {
            File.AppendAllText(githubOutput, $"semver={versionInfo.SemVer}{Environment.NewLine}");
            File.AppendAllText(githubOutput, $"fullSemVer={versionInfo.FullSemVer}{Environment.NewLine}");
            File.AppendAllText(githubOutput, $"informationalVersion={versionInfo.InformationalVersion}{Environment.NewLine}");
        }
    }
}
