using System.Collections.Generic;
using System.Linq;
using Timtek.GitFlowVersion.Git;
using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.Scenarios;

/// <summary>
/// Parses the output of <c>git fast-export --all --no-data</c> and produces a
/// sequence of <see cref="BuilderStep"/> operations that reconstruct the
/// branching, tagging, merging, and commit structure.
/// </summary>
public static class GitHistoryAnalyzer
{
    /// <summary>Analyzes the repository at <paramref name="directory"/> and returns the builder steps.</summary>
    public static List<BuilderStep> AnalyzeHistory(string directory)
    {
        var currentBranch = GitCommandRunner.RunCommand("rev-parse --abbrev-ref HEAD", directory);
        var fastExportOutput = GitCommandRunner.RunCommand("fast-export --all --no-data", directory);

        var parseResult = ParseFastExport(fastExportOutput);
        return ProduceSteps(parseResult, currentBranch);
    }

    private sealed class CommitRecord
    {
        public string Mark { get; set; } = "";
        public string? ParentMark { get; set; }
        public List<string> MergeMarks { get; } = new List<string>();
        public string? PreferredBranch { get; set; }
    }

    private sealed class TagRecord
    {
        public string Name { get; set; } = "";
        public string CommitMark { get; set; } = "";
    }

    private sealed class ParseResult
    {
        public List<CommitRecord> Commits { get; } = new List<CommitRecord>();
        public List<TagRecord> Tags { get; } = new List<TagRecord>();
        public Dictionary<string, string> BranchTips { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static ParseResult ParseFastExport(string output)
    {
        var result = new ParseResult();
        var lines = output.Split(new[] { '\n' });
        var position = 0;

        while (position < lines.Length)
        {
            var line = lines[position].TrimEnd('\r');

            if (line.StartsWith("commit ", StringComparison.Ordinal))
            {
                ParseCommitRecord(lines, ref position, line, result);
                continue;
            }

            if (line.StartsWith("tag ", StringComparison.Ordinal))
            {
                ParseAnnotatedTag(lines, ref position, line, result);
                continue;
            }

            if (line.StartsWith("reset refs/tags/", StringComparison.Ordinal))
            {
                ParseLightweightTag(lines, ref position, line, result);
                continue;
            }

            if (line.StartsWith("reset ", StringComparison.Ordinal))
            {
                ParseBranchReset(lines, ref position, line, result);
                continue;
            }

            position++;
        }

        return result;
    }

    private static string? ExtractBranchName(string refPath)
    {
        const string headsPrefix = "refs/heads/";
        const string remotesPrefix = "refs/remotes/origin/";

        if (refPath.StartsWith(headsPrefix, StringComparison.Ordinal))
            return refPath.Substring(headsPrefix.Length);

        if (refPath.StartsWith(remotesPrefix, StringComparison.Ordinal))
        {
            var name = refPath.Substring(remotesPrefix.Length);
            return string.Equals(name, "HEAD", StringComparison.OrdinalIgnoreCase) ? null : name;
        }

        return null;
    }

    private static void ParseCommitRecord(string[] lines, ref int position, string headerLine, ParseResult result)
    {
        var refPath = headerLine.Substring("commit ".Length).Trim();
        var commit = new CommitRecord { PreferredBranch = ExtractBranchName(refPath) };
        position++;

        while (position < lines.Length)
        {
            var line = lines[position].TrimEnd('\r');

            if (string.IsNullOrEmpty(line))
            {
                position++;
                break;
            }

            if (IsRecordStart(line))
                break;

            if (line.StartsWith("mark :", StringComparison.Ordinal))
            {
                commit.Mark = line.Substring("mark :".Length);
            }
            else if (line.StartsWith("from :", StringComparison.Ordinal))
            {
                commit.ParentMark = line.Substring("from :".Length);
            }
            else if (line.StartsWith("merge :", StringComparison.Ordinal))
            {
                commit.MergeMarks.Add(line.Substring("merge :".Length));
            }
            else if (line.StartsWith("data ", StringComparison.Ordinal))
            {
                SkipDataBlock(lines, ref position);
                continue;
            }

            position++;
        }

        if (!string.IsNullOrEmpty(commit.Mark))
        {
            result.Commits.Add(commit);

            if (!string.IsNullOrWhiteSpace(commit.PreferredBranch))
            {
                var preferredBranch = commit.PreferredBranch;
                if (preferredBranch != null)
                    result.BranchTips[preferredBranch] = commit.Mark;
            }
        }
    }

    private static void ParseAnnotatedTag(string[] lines, ref int position, string headerLine, ParseResult result)
    {
        var tagName = headerLine.Substring("tag ".Length).Trim();
        string? commitMark = null;
        position++;

        while (position < lines.Length)
        {
            var line = lines[position].TrimEnd('\r');

            if (string.IsNullOrEmpty(line))
            {
                position++;
                break;
            }

            if (IsRecordStart(line))
                break;

            if (line.StartsWith("from :", StringComparison.Ordinal))
            {
                commitMark = line.Substring("from :".Length);
            }
            else if (line.StartsWith("data ", StringComparison.Ordinal))
            {
                SkipDataBlock(lines, ref position);
                continue;
            }

            position++;
        }

        if (commitMark != null && IsVersionTag(tagName))
            result.Tags.Add(new TagRecord { Name = tagName, CommitMark = commitMark });
    }

    private static void ParseLightweightTag(string[] lines, ref int position, string headerLine, ParseResult result)
    {
        var tagName = headerLine.Substring("reset refs/tags/".Length).Trim();
        string? commitMark = null;
        position++;

        while (position < lines.Length)
        {
            var line = lines[position].TrimEnd('\r');

            if (string.IsNullOrEmpty(line))
            {
                position++;
                break;
            }

            if (IsRecordStart(line))
                break;

            if (line.StartsWith("from :", StringComparison.Ordinal))
                commitMark = line.Substring("from :".Length);

            position++;
        }

        if (commitMark != null && IsVersionTag(tagName))
            result.Tags.Add(new TagRecord { Name = tagName, CommitMark = commitMark });
    }

    private static void ParseBranchReset(string[] lines, ref int position, string headerLine, ParseResult result)
    {
        var refPath = headerLine.Substring("reset ".Length).Trim();
        var branchName = ExtractBranchName(refPath);
        string? commitMark = null;
        position++;

        while (position < lines.Length)
        {
            var line = lines[position].TrimEnd('\r');

            if (string.IsNullOrEmpty(line))
            {
                position++;
                break;
            }

            if (IsRecordStart(line))
                break;

            if (line.StartsWith("from :", StringComparison.Ordinal))
                commitMark = line.Substring("from :".Length);

            position++;
        }

        if (!string.IsNullOrWhiteSpace(branchName) && !string.IsNullOrWhiteSpace(commitMark))
        {
            var normalizedBranchName = branchName;
            var normalizedCommitMark = commitMark;
            if (normalizedBranchName != null && normalizedCommitMark != null)
                result.BranchTips[normalizedBranchName] = normalizedCommitMark;
        }
    }

    private static bool IsRecordStart(string line) =>
        line.StartsWith("commit ", StringComparison.Ordinal) ||
        line.StartsWith("tag ", StringComparison.Ordinal) ||
        line.StartsWith("reset ", StringComparison.Ordinal) ||
        line == "blob";

    private static void SkipDataBlock(string[] lines, ref int position)
    {
        var dataLine = lines[position].TrimEnd('\r');
        int remainingBytes;
        try
        {
            remainingBytes = int.Parse(dataLine.Substring("data ".Length));
        }
        catch
        {
            position++;
            return;
        }

        position++;

        while (position < lines.Length && remainingBytes > 0)
        {
            var line = lines[position].TrimEnd('\r');
            var lineWithNewlineBytes = line.Length + 1;

            if (remainingBytes >= lineWithNewlineBytes)
            {
                remainingBytes -= lineWithNewlineBytes;
                position++;
                continue;
            }

            if (remainingBytes < line.Length)
            {
                lines[position] = line.Substring(remainingBytes);
                return;
            }

            lines[position] = string.Empty;
            return;
        }
    }

    private static int GetBranchPriority(string branchName)
    {
        var type = BranchClassifier.Classify(branchName);
        return type switch
        {
            BranchType.Main => 0,
            BranchType.Develop => 1,
            BranchType.Release => 2,
            BranchType.Hotfix => 3,
            _ => 4
        };
    }

    private static Dictionary<string, HashSet<string>> BuildFirstParentChains(
        Dictionary<string, string> branchTips,
        Dictionary<string, CommitRecord> commitByMark)
    {
        var chains = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in branchTips)
        {
            var chain = new HashSet<string>();
            var currentMark = kvp.Value;

            while (!string.IsNullOrWhiteSpace(currentMark) && chain.Add(currentMark))
            {
                if (!commitByMark.TryGetValue(currentMark, out var commit) || commit.ParentMark == null)
                    break;

                currentMark = commit.ParentMark;
            }

            chains[kvp.Key] = chain;
        }

        return chains;
    }

    private static Dictionary<string, string> AssignCommitsToBranches(
        List<CommitRecord> commits,
        Dictionary<string, HashSet<string>> firstParentChains)
    {
        var branchesByPriority = firstParentChains.Keys
            .OrderBy(GetBranchPriority)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (branchesByPriority.Count == 0)
        {
            branchesByPriority = commits
                .Where(c => !string.IsNullOrWhiteSpace(c.PreferredBranch))
                .Select(c => c.PreferredBranch!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetBranchPriority)
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var assignment = new Dictionary<string, string>();

        foreach (var commit in commits)
        {
            if (!string.IsNullOrWhiteSpace(commit.PreferredBranch))
            {
                assignment[commit.Mark] = commit.PreferredBranch!;
                continue;
            }

            string? bestBranch = null;
            var bestPriority = int.MaxValue;

            foreach (var branch in branchesByPriority)
            {
                if (!firstParentChains.TryGetValue(branch, out var chain) || !chain.Contains(commit.Mark))
                    continue;

                var priority = GetBranchPriority(branch);
                if (priority >= bestPriority)
                    continue;

                bestPriority = priority;
                bestBranch = branch;
            }

            assignment[commit.Mark] = bestBranch ?? branchesByPriority.FirstOrDefault() ?? "main";
        }

        return assignment;
    }

    private static string DetermineRootBranch(
        List<CommitRecord> commits,
        Dictionary<string, string> commitBranch,
        IEnumerable<string> knownBranches)
    {
        var rootCandidates = commits
            .Where(c => c.ParentMark == null)
            .Select(c => commitBranch[c.Mark])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetBranchPriority)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rootCandidates.Count > 0)
            return rootCandidates[0];

        return knownBranches
            .OrderBy(GetBranchPriority)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? "main";
    }

    private static List<BuilderStep> ProduceSteps(ParseResult parseResult, string currentBranch)
    {
        var commits = parseResult.Commits;
        if (commits.Count == 0)
            return new List<BuilderStep>();

        var commitByMark = commits.ToDictionary(c => c.Mark);
        var firstParentChains = BuildFirstParentChains(parseResult.BranchTips, commitByMark);
        var commitBranch = AssignCommitsToBranches(commits, firstParentChains);

        var tagsByCommitMark = new Dictionary<string, List<string>>();
        foreach (var tag in parseResult.Tags)
        {
            if (!tagsByCommitMark.TryGetValue(tag.CommitMark, out var list))
            {
                list = new List<string>();
                tagsByCommitMark[tag.CommitMark] = list;
            }
            list.Add(tag.Name);
        }

        var branchCommits = new Dictionary<string, List<CommitRecord>>(StringComparer.OrdinalIgnoreCase);
        foreach (var commit in commits)
        {
            var branch = commitBranch[commit.Mark];
            if (!branchCommits.TryGetValue(branch, out var list))
            {
                list = new List<CommitRecord>();
                branchCommits[branch] = list;
            }
            list.Add(commit);
        }

        var commitLocation = new Dictionary<string, (string branch, int index)>();
        foreach (var kvp in branchCommits)
        {
            for (var i = 0; i < kvp.Value.Count; i++)
                commitLocation[kvp.Value[i].Mark] = (kvp.Key, i);
        }

        var rootCommit = commits.FirstOrDefault(c => c.ParentMark == null);
        var rootBranch = rootCommit != null
            ? commitBranch[rootCommit.Mark]
            : branchCommits.Keys.OrderBy(GetBranchPriority).ThenBy(name => name, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? "main";

        var branchCreationPoints = new Dictionary<string, List<string>>();
        foreach (var kvp in branchCommits)
        {
            var branch = kvp.Key;
            var branchList = kvp.Value;
            if (string.Equals(branch, rootBranch, StringComparison.OrdinalIgnoreCase))
                continue;
            if (branchList.Count == 0 || branchList[0].ParentMark == null)
                continue;

            var parentMark = branchList[0].ParentMark;
            if (parentMark == null)
                continue;

            if (commitLocation.TryGetValue(parentMark, out var parentLoc)
                && string.Equals(parentLoc.branch, branch, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!branchCreationPoints.TryGetValue(parentMark, out var children))
            {
                children = new List<string>();
                branchCreationPoints[parentMark] = children;
            }
            children.Add(branch);
        }

        foreach (var kvp in branchCreationPoints)
            kvp.Value.Sort((a, b) => GetBranchPriority(a).CompareTo(GetBranchPriority(b)));

        var processedIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var branch in branchCommits.Keys)
            processedIndex[branch] = -1;

        var steps = new List<BuilderStep>();
        var builderBranch = rootBranch;
        var pendingCommits = 0;
        var createdBranches = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootBranch };

        void FlushPending()
        {
            if (pendingCommits <= 0)
                return;

            steps.Add(BuilderStep.Commits(pendingCommits));
            pendingCommits = 0;
        }

        void EnsureOnBranch(string targetBranch)
        {
            if (string.Equals(builderBranch, targetBranch, StringComparison.OrdinalIgnoreCase))
                return;

            FlushPending();
            steps.Add(BuilderStep.Checkout(targetBranch));
            builderBranch = targetBranch;
        }

        void ProcessBranchTo(string branch, int targetIndex)
        {
            if (!branchCommits.TryGetValue(branch, out var branchList))
                return;

            if (targetIndex >= branchList.Count)
                targetIndex = branchList.Count - 1;

            while (processedIndex[branch] < targetIndex)
            {
                var i = processedIndex[branch] + 1;
                processedIndex[branch] = i;
                var commit = branchList[i];

                if (commit.ParentMark == null)
                {
                    steps.Add(BuilderStep.InitialCommit());
                    builderBranch = branch;
                }
                else if (commit.MergeMarks.Count > 0)
                {
                    foreach (var mergeMark in commit.MergeMarks)
                    {
                        if (commitLocation.TryGetValue(mergeMark, out var sourceLoc)
                            && processedIndex.TryGetValue(sourceLoc.branch, out var sourceProgress)
                            && sourceProgress < sourceLoc.index)
                        {
                            ProcessBranchTo(sourceLoc.branch, sourceLoc.index);
                        }
                    }

                    EnsureOnBranch(branch);
                    FlushPending();

                    foreach (var mergeMark in commit.MergeMarks)
                    {
                        var sourceBranch = commitLocation.TryGetValue(mergeMark, out var loc)
                            ? loc.branch
                            : commitBranch.TryGetValue(mergeMark, out var mappedBranch) ? mappedBranch : "unknown";

                        steps.Add(BuilderStep.Merge(sourceBranch));
                    }
                }
                else
                {
                    EnsureOnBranch(branch);
                    pendingCommits++;
                }

                if (tagsByCommitMark.TryGetValue(commit.Mark, out var tags))
                {
                    FlushPending();
                    foreach (var tag in tags)
                        steps.Add(BuilderStep.Tag(tag));
                }

                if (branchCreationPoints.TryGetValue(commit.Mark, out var children))
                {
                    FlushPending();
                    foreach (var child in children)
                    {
                        if (createdBranches.Contains(child))
                            continue;

                        EnsureOnBranch(branch);
                        steps.Add(BuilderStep.Branch(child));
                        createdBranches.Add(child);
                        builderBranch = child;

                        if (branchCommits.TryGetValue(child, out var childList))
                            ProcessBranchTo(child, childList.Count - 1);
                    }
                }
            }

            FlushPending();
        }

        if (branchCommits.ContainsKey(rootBranch))
            ProcessBranchTo(rootBranch, branchCommits[rootBranch].Count - 1);

        foreach (var branch in branchCommits.Keys.OrderBy(GetBranchPriority))
        {
            if (!branchCommits.TryGetValue(branch, out var branchList))
                continue;

            if (processedIndex[branch] < branchList.Count - 1)
                ProcessBranchTo(branch, branchList.Count - 1);
        }

        FlushPending();

        if (!string.Equals(builderBranch, currentBranch, StringComparison.OrdinalIgnoreCase))
            steps.Add(BuilderStep.Checkout(currentBranch));

        return steps;
    }

    private static bool IsVersionTag(string tag)
    {
        var name = tag;
        if (name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(1);

        var parts = name.Split('.');
        return parts.Length == 3 && parts.All(p => int.TryParse(p, out _));
    }
}
