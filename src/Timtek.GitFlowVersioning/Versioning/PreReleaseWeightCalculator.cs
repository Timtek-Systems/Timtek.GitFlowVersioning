namespace Timtek.GitFlowVersion.Versioning;

/// <summary>Provides GitFlow branch pre-release weights compatible with GitVersion defaults.</summary>
public static class PreReleaseWeightCalculator
{
    /// <summary>Gets the configured pre-release weight for the supplied Git branch name.</summary>
    /// <param name="branchName">The Git branch name.</param>
    /// <returns>The branch-specific pre-release weight.</returns>
    public static int GetWeight(string branchName) => GetWeight(BranchClassifier.Classify(branchName));

    /// <summary>Gets the configured pre-release weight for the supplied branch type.</summary>
    /// <param name="branchType">The classified branch type.</param>
    /// <returns>The branch-specific pre-release weight.</returns>
    public static int GetWeight(BranchType branchType) =>
        branchType switch
        {
            BranchType.Main => 55000,
            BranchType.Develop => 0,
            BranchType.Release => 30000,
            BranchType.Hotfix => 30000,
            _ => 30000
        };
}
