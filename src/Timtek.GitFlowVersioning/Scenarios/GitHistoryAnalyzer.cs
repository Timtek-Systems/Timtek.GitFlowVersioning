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
        public string Branch { get; set; } = "";
        public string? ParentMark { get; set; }
        public List<string> MergeMarks { get; } = new List<string>();
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
    }

    /// <summary>
    /// Parses a <c>git fast-export --all --no-data</c> output stream into
    /// commit and tag records. Commits appear in the topological order that
    /// fast-export emits them. Branch information is taken directly from the
    /// <c>commit refs/heads/&lt;branch&gt;</c> lines, which is authoritative.
    /// </summary>
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
        var branch = ExtractBranchName(refPath);

        // Skip commits on refs we don't recognise (notes, stash, etc.)
        if (branch == null)
        {
            position++;
            SkipRecordBody(lines, ref position);
            return;
        }

        var commit = new CommitRecord { Branch = branch };
        position++;

        while (position < lines.Length)
        {
            var line = lines[position].TrimEnd('\r');

            if (string.IsNullOrEmpty(line))
            {
                position++;
                break;
            }

            // A new top-level record means this record ended without a blank line
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

        result.Commits.Add(commit);
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
            {
                commitMark = line.Substring("from :".Length);
            }

            position++;
        }

        if (commitMark != null && IsVersionTag(tagName))
            result.Tags.Add(new TagRecord { Name = tagName, CommitMark = commitMark });
    }

    private static bool IsRecordStart(string line) =>
        line.StartsWith("commit ", StringComparison.Ordinal) ||
        line.StartsWith("tag ", StringComparison.Ordinal) ||
        line.StartsWith("reset ", StringComparison.Ordinal) ||
        line == "blob";

    private static void SkipRecordBody(string[] lines, ref int position)
    {
        while (position < lines.Length)
        {
            var line = lines[position].TrimEnd('\r');

            if (IsRecordStart(line))
                return;

            if (line.StartsWith("data ", StringComparison.Ordinal))
            {
                SkipDataBlock(lines, ref position);
                continue;
            }

            position++;
        }
    }

    /// <summary>
    /// Skips past a <c>data &lt;N&gt;</c> block. The current position must be
    /// on the <c>data</c> line. After this method returns, <paramref name="position"/>
    /// points to the first line after the data content.
    /// </summary>
    private static void SkipDataBlock(string[] lines, ref int position)
    {
        var dataLine = lines[position].TrimEnd('\r');
        if (!int.TryParse(dataLine.Substring("data ".Length), out var byteCount))
        {
            position++;
            return;
        }

        position++; // move past the "data N" line

        if (byteCount == 0)
            return;

        // Consume lines until we have accounted for byteCount bytes.
        // Each consumed line contributes its length + 1 (for the LF separator).
        var consumed = 0;
        while (position < lines.Length && consumed < byteCount)
        {
            consumed += lines[position].TrimEnd('\r').Length + 1; // +1 for the LF
            position++;
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

    /// <summary>
    /// Converts parsed fast-export records into an ordered list of
    /// <see cref="BuilderStep"/> operations.
    /// </summary>
    private static List<BuilderStep> ProduceSteps(ParseResult parseResult, string currentBranch)
    {
        var commits = parseResult.Commits;
        if (commits.Count == 0)
            return new List<BuilderStep>();

        // Build mark → commit lookup
        var commitByMark = new Dictionary<string, CommitRecord>();
        foreach (var commit in commits)
            commitByMark[commit.Mark] = commit;

        // Build mark → list of version tags
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

        // Group commits by branch, preserving fast-export topological order
        var branchCommits = new Dictionary<string, List<CommitRecord>>(StringComparer.OrdinalIgnoreCase);
        foreach (var commit in commits)
        {
            if (!branchCommits.TryGetValue(commit.Branch, out var list))
            {
                list = new List<CommitRecord>();
                branchCommits[commit.Branch] = list;
            }
            list.Add(commit);
        }

        // Build mark → (branch, index) for efficient lookup
        var commitLocation = new Dictionary<string, (string branch, int index)>();
        foreach (var kvp in branchCommits)
        {
            for (var i = 0; i < kvp.Value.Count; i++)
                commitLocation[kvp.Value[i].Mark] = (kvp.Key, i);
        }

        // Determine root branch (branch of the first commit with no parent)
        var rootCommit = commits.FirstOrDefault(c => c.ParentMark == null);
        var rootBranch = rootCommit?.Branch ?? "main";

        // Pre-compute branch creation points: parent mark → child branches to create
        var branchCreationPoints = new Dictionary<string, List<string>>();
        foreach (var kvp in branchCommits)
        {
            var branch = kvp.Key;
            var branchList = kvp.Value;
            if (string.Equals(branch, rootBranch, StringComparison.OrdinalIgnoreCase))
                continue;
            if (branchList.Count == 0 || branchList[0].ParentMark == null)
                continue;

            var parentMark = branchList[0].ParentMark!;

            // Only register as a branch creation if the parent is on a different branch
            if (commitLocation.TryGetValue(parentMark, out var parentLoc) &&
                string.Equals(parentLoc.branch, branch, StringComparison.OrdinalIgnoreCase))
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

        // Per-branch progress tracker
        var processedIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var branch in branchCommits.Keys)
            processedIndex[branch] = -1;

        var steps = new List<BuilderStep>();
        var builderBranch = rootBranch;
        var pendingCommits = 0;
        var createdBranches = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootBranch };

        void FlushPending()
        {
            if (pendingCommits <= 0) return;
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
                    // Advance each merge source branch before merging
                    foreach (var mergeMark in commit.MergeMarks)
                    {
                        if (commitLocation.TryGetValue(mergeMark, out var sourceLoc) &&
                            processedIndex.TryGetValue(sourceLoc.branch, out var sourceProgress) &&
                            sourceProgress < sourceLoc.index)
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
                            : "unknown";
                        steps.Add(BuilderStep.Merge(sourceBranch));
                    }
                }
                else
                {
                    EnsureOnBranch(branch);
                    pendingCommits++;
                }

                // Tags on this commit
                if (tagsByCommitMark.TryGetValue(commit.Mark, out var tags))
                {
                    FlushPending();
                    foreach (var tag in tags)
                        steps.Add(BuilderStep.Tag(tag));
                }

                // Eagerly process child branches created at this commit
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

        // Process root branch first (depth-first recursion handles reachable branches)
        if (branchCommits.ContainsKey(rootBranch))
            ProcessBranchTo(rootBranch, branchCommits[rootBranch].Count - 1);

        // Process any branches not reached by the depth-first walk
        foreach (var branch in branchCommits.Keys.OrderBy(GetBranchPriority))
        {
            if (!branchCommits.TryGetValue(branch, out var bc))
                continue;
            if (processedIndex[branch] < bc.Count - 1)
                ProcessBranchTo(branch, bc.Count - 1);
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
