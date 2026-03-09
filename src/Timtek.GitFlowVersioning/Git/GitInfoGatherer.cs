namespace Timtek.GitFlowVersioning.Git;

/// <summary>Gathers git commit information from a repository directory.</summary>
public static class GitInfoGatherer
{
    /// <summary>Gathers git commit information from the repository at or above <paramref name="directory"/>.</summary>
    /// <param name="directory">A directory within a git repository.</param>
    /// <returns>A populated <see cref="Versioning.GitCommitInfo"/>.</returns>
    public static Versioning.GitCommitInfo Gather(string directory)
    {
        var repoRoot = FindRepoRoot(directory);
        var sha = GitCommandRunner.RunCommand("rev-parse HEAD", repoRoot);
        var branchName = GitCommandRunner.RunCommand("rev-parse --abbrev-ref HEAD", repoRoot);

        if (branchName == "HEAD")
            branchName = TryGetBranchNameFromDetachedHead(repoRoot, sha);

        var (baseTag, distance, hasTag) = GetVersionFromDescribe(repoRoot);

        return new Versioning.GitCommitInfo
        {
            Sha = sha,
            BranchName = branchName,
            BaseVersionTag = baseTag,
            CommitDistance = distance,
            HasTag = hasTag
        };
    }

    private static string FindRepoRoot(string directory)
    {
        try
        {
            return GitCommandRunner.RunCommand("rev-parse --show-toplevel", directory);
        }
        catch
        {
            return directory;
        }
    }

    private static string TryGetBranchNameFromDetachedHead(string repoRoot, string sha)
    {
        try
        {
            var result = GitCommandRunner.RunCommand($"name-rev --name-only {sha}", repoRoot);
            return result.Replace("remotes/origin/", "").Trim();
        }
        catch
        {
            return "HEAD";
        }
    }

    private static (string baseTag, int distance, bool hasTag) GetVersionFromDescribe(string repoRoot)
    {
        string describeOutput;
        try
        {
            describeOutput = GitCommandRunner.RunCommand(@"describe --tags --long --match ""v*.*.*"" HEAD", repoRoot);
        }
        catch
        {
            try
            {
                describeOutput = GitCommandRunner.RunCommand(@"describe --tags --long --match ""*.*.*"" HEAD", repoRoot);
            }
            catch
            {
                return GetFallbackFromCommitCount(repoRoot);
            }
        }

        return ParseDescribeOutput(describeOutput);
    }

    private static (string baseTag, int distance, bool hasTag) GetFallbackFromCommitCount(string repoRoot)
    {
        try
        {
            var countStr = GitCommandRunner.RunCommand("rev-list --count HEAD", repoRoot);
            var count = int.TryParse(countStr, out var c) ? c : 0;
            return ("0.1.0", count, false);
        }
        catch
        {
            return ("0.1.0", 0, false);
        }
    }

    internal static (string baseTag, int distance, bool hasTag) ParseDescribeOutput(string describeOutput)
    {
        // format: "v1.2.3-5-gabcdef" or "1.2.3-5-gabcdef"
        var lastDash = describeOutput.LastIndexOf('-');
        if (lastDash < 0) return ("0.1.0", 0, true);

        var secondLastDash = describeOutput.LastIndexOf('-', lastDash - 1);
        if (secondLastDash < 0) return ("0.1.0", 0, true);

        var tagPart = describeOutput.Substring(0, secondLastDash).TrimStart('v', 'V');
        var distancePart = describeOutput.Substring(secondLastDash + 1, lastDash - secondLastDash - 1);

        var distance = int.TryParse(distancePart, out var d) ? d : 0;
        return (tagPart, distance, true);
    }
}
