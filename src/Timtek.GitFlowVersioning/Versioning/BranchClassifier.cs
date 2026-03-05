namespace Timtek.GitFlowVersioning.Versioning;

/// <summary>Classifies a git branch name into a <see cref="BranchType"/> according to GitFlow conventions.</summary>
public static class BranchClassifier
{
    /// <summary>Classifies <paramref name="branchName"/> into its GitFlow <see cref="BranchType"/>.</summary>
    /// <param name="branchName">The full branch name (e.g. "feature/foo", "main").</param>
    /// <returns>The <see cref="BranchType"/> for the branch.</returns>
    public static BranchType Classify(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            return BranchType.Other;

        var name = branchName.Trim();

        if (name.Equals("main", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("master", StringComparison.OrdinalIgnoreCase))
            return BranchType.Main;

        if (name.Equals("develop", StringComparison.OrdinalIgnoreCase))
            return BranchType.Develop;

        if (name.StartsWith("release/", StringComparison.OrdinalIgnoreCase))
            return BranchType.Release;

        if (name.StartsWith("hotfix/", StringComparison.OrdinalIgnoreCase))
            return BranchType.Hotfix;

        return BranchType.Other;
    }
}
