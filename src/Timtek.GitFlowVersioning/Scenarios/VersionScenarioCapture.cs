using Timtek.GitFlowVersion.Git;
using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.Scenarios;

/// <summary>Captures repository history and expected version from a real Git repository.</summary>
public static class VersionScenarioCapture
{
    /// <summary>Captures the repository history needed to reconstruct a test repository.</summary>
    /// <param name="directory">A directory within the Git repository to capture.</param>
    /// <param name="scenarioName">An optional stable name for the captured scenario.</param>
    /// <returns>A captured scenario containing builder steps and expected version.</returns>
    public static CapturedVersionScenario Capture(string directory, string scenarioName = "")
    {
        if (directory is null)
            throw new ArgumentNullException(nameof(directory));

        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("The repository directory must not be empty.", nameof(directory));

        var steps = GitHistoryAnalyzer.AnalyzeHistory(directory);
        var commitInfo = GitInfoGatherer.Gather(directory);
        var expectedVersion = VersionCalculator.Calculate(commitInfo);

        return new CapturedVersionScenario
        {
            Scenario = ResolveScenarioName(scenarioName, expectedVersion),
            Steps = steps,
            ExpectedVersion = expectedVersion
        };
    }

    private static string ResolveScenarioName(string scenarioName, VersionInfo expectedVersion)
    {
        if (!string.IsNullOrWhiteSpace(scenarioName))
            return scenarioName.Trim();

        var branchName = string.IsNullOrWhiteSpace(expectedVersion.EscapedBranchName)
            ? "detached-head"
            : expectedVersion.EscapedBranchName;
        var semVer = expectedVersion.SemVer.Replace('+', '-');

        return $"{branchName}-{semVer}";
    }
}
