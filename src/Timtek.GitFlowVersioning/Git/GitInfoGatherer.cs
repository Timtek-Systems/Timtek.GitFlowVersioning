namespace Timtek.GitFlowVersion.Git;

using System.Text.RegularExpressions;

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

        branchName = NormalizeBranchName(branchName);

        var (baseTag, distanceFromTag, hasTag) = GetVersionFromDescribe(repoRoot);
        var distance = GetBranchAwareDistance(repoRoot, branchName, distanceFromTag);

        return new Versioning.GitCommitInfo
        {
            Sha = sha,
            BranchName = branchName,
            BaseVersionTag = baseTag,
            CommitDistance = distance,
            HasTag = hasTag
        };
    }

    private static string NormalizeBranchName(string branchName)
    {
        var normalized = branchName.Trim();

        normalized = StripPrefix(normalized, "refs/heads/");
        normalized = StripPrefix(normalized, "refs/remotes/origin/");
        normalized = StripPrefix(normalized, "remotes/origin/");
        normalized = StripPrefix(normalized, "origin/");
        normalized = StripPrefix(normalized, "heads/");

        var detachedSuffixIndex = normalized.IndexOfAny(new[] { '~', '^' });
        if (detachedSuffixIndex > 0)
            normalized = normalized.Substring(0, detachedSuffixIndex);

        return normalized;
    }

    private static string StripPrefix(string value, string prefix) =>
        value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value.Substring(prefix.Length)
            : value;

    private static int GetBranchAwareDistance(string repoRoot, string branchName, int distanceFromTag)
    {
        if (branchName.StartsWith("release/", StringComparison.OrdinalIgnoreCase))
            return TryGetDistanceFromMergeBase(repoRoot, "develop") ?? distanceFromTag;

        if (branchName.StartsWith("hotfix/", StringComparison.OrdinalIgnoreCase))
        {
            var hotfixDistance = TryGetDistanceFromMergeBase(repoRoot, "main")
                                 ?? TryGetDistanceFromMergeBase(repoRoot, "master");
            return hotfixDistance ?? distanceFromTag;
        }

        return distanceFromTag;
    }

    private static int? TryGetDistanceFromMergeBase(string repoRoot, string sourceBranch)
    {
        try
        {
            var mergeBase = GitCommandRunner.RunCommand($"merge-base HEAD {sourceBranch}", repoRoot);
            var distanceText = GitCommandRunner.RunCommand($"rev-list --count {mergeBase}..HEAD", repoRoot);
            return int.TryParse(distanceText, out var distance) ? distance : null;
        }
        catch
        {
            return null;
        }
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
            return GitCommandRunner.RunCommand($"name-rev --name-only {sha}", repoRoot);
        }
        catch
        {
            return "HEAD";
        }
    }

    // These are git --match glob patterns (not .NET regular expressions).
    // We keep them intentionally broad for candidate selection, then apply
    // strict semantic version validation in C# via SemanticVersionTagPattern.
    private static readonly string[] GitGlobVersionTagPatterns =
    [
        @"v[0-9]*.*.*",
        @"V[0-9]*.*.*",
        @"[0-9]*.*.*"
    ];

    private static readonly Regex SemanticVersionTagPattern = new(@"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant);

    private static (string baseTag, int distance, bool hasTag) GetVersionFromDescribe(string repoRoot)
    {
        foreach (var pattern in GitGlobVersionTagPatterns)
        {
            try
            {
                var describeOutput = GitCommandRunner.RunCommand($"""describe --tags --long --match "{pattern}" HEAD""", repoRoot);
                var parsed = ParseDescribeOutput(describeOutput);
                if (IsSemanticVersionTag(parsed.baseTag))
                    return parsed;
            }
            catch
            {
                // pattern did not match any reachable tag; try next pattern
            }
        }

        var nearestSemanticTag = TryGetNearestSemanticVersionTag(repoRoot);
        if (nearestSemanticTag is { } tag)
            return tag;

        return GetFallbackFromCommitCount(repoRoot);
    }

    private static bool IsSemanticVersionTag(string tagName) => SemanticVersionTagPattern.IsMatch(tagName);

    private static (string baseTag, int distance, bool hasTag)? TryGetNearestSemanticVersionTag(string repoRoot)
    {
        try
        {
            var tags = GitCommandRunner.RunCommand("tag --merged HEAD --list", repoRoot)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(tagName => new
                {
                    TagName = tagName,
                    NormalizedTagName = tagName.TrimStart('v', 'V')
                })
                .Where(tag => IsSemanticVersionTag(tag.NormalizedTagName))
                .Select(tag => new
                {
                    tag.NormalizedTagName,
                    Distance = TryGetDistanceFromTag(repoRoot, tag.TagName),
                    Version = Version.Parse(tag.NormalizedTagName)
                })
                .Where(tag => tag.Distance.HasValue)
                .OrderBy(tag => tag.Distance!.Value)
                .ThenByDescending(tag => tag.Version)
                .FirstOrDefault();

            return tags is null ? null : (tags.NormalizedTagName, tags.Distance!.Value, true);
        }
        catch
        {
            return null;
        }
    }

    private static int? TryGetDistanceFromTag(string repoRoot, string tagName)
    {
        try
        {
            var distanceText = GitCommandRunner.RunCommand($"rev-list --count \"{tagName}\"..HEAD", repoRoot);
            return int.TryParse(distanceText, out var distance) ? distance : null;
        }
        catch
        {
            return null;
        }
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
