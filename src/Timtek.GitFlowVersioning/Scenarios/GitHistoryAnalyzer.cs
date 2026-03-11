using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Timtek.GitFlowVersion.Git;
using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.Scenarios;

/// <summary>
/// Walks the commit history of a real Git repository and produces a sequence
/// of <see cref="BuilderStep"/> operations that reconstruct the branching,
/// tagging, merging, and commit structure.
/// </summary>
public static class GitHistoryAnalyzer
{
    /// <summary>Analyzes the repository at <paramref name="directory"/> and returns the builder steps.</summary>
    public static List<BuilderStep> AnalyzeHistory(string directory)
    {
        var currentBranch = GitCommandRunner.RunCommand("rev-parse --abbrev-ref HEAD", directory);
        var commits = GetTopologicalCommits(directory);
        var branchTips = GetBranchTips(directory);
        var firstParentChains = BuildFirstParentChains(directory, branchTips);
        var commitBranch = AssignCommitsToBranches(commits, firstParentChains);
        var commitSubjects = GetCommitSubjects(directory, commits);
        DetectOrphanBranches(commits, commitBranch, firstParentChains, commitSubjects);
        var commitTags = BuildTagMap(directory, commits);

        return ProduceSteps(commits, commitBranch, commitTags, branchTips, currentBranch);
    }

    private sealed class CommitInfo
    {
        public string Hash { get; set; } = "";
        public List<string> Parents { get; set; } = new List<string>();
    }

    private static List<CommitInfo> GetTopologicalCommits(string directory)
    {
        var output = GitCommandRunner.RunCommand(@"log --all --topo-order --reverse --format=%H|%P", directory);
        var result = new List<CommitInfo>();

        foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = line.IndexOf('|');
            if (separatorIndex <= 0) continue;

            var hash = line.Substring(0, separatorIndex).Trim();
            var parentsPart = line.Substring(separatorIndex + 1).Trim();
            var parents = string.IsNullOrEmpty(parentsPart)
                ? new List<string>()
                : parentsPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            result.Add(new CommitInfo { Hash = hash, Parents = parents });
        }

        return result;
    }

    /// <summary>
    /// Gets branch tips from local branches, supplemented by remote tracking
    /// branches when no local equivalent exists (e.g. origin/main when there
    /// is no local main).
    /// </summary>
    private static Dictionary<string, string> GetBranchTips(string directory)
    {
        var tips = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Local branches first
        try
        {
            var output = GitCommandRunner.RunCommand(
                @"for-each-ref --format=%(objectname)|%(refname:short) refs/heads/", directory);
            ParseBranchTips(output, tips);
        }
        catch
        {
            // No local branches
        }

        // Add remote tracking branches where no local equivalent exists
        try
        {
            var output = GitCommandRunner.RunCommand(
                @"for-each-ref --format=%(objectname)|%(refname:short) refs/remotes/origin/", directory);
            foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var separatorIndex = line.IndexOf('|');
                if (separatorIndex <= 0) continue;

                var commitHash = line.Substring(0, separatorIndex).Trim();
                var remoteName = line.Substring(separatorIndex + 1).Trim();

                // Strip "origin/" prefix to get canonical branch name
                if (!remoteName.StartsWith("origin/", StringComparison.OrdinalIgnoreCase)) continue;
                var localName = remoteName.Substring("origin/".Length);
                if (string.Equals(localName, "HEAD", StringComparison.OrdinalIgnoreCase)) continue;

                if (!tips.ContainsKey(localName))
                    tips[localName] = commitHash;
            }
        }
        catch
        {
            // No remote branches
        }

        return tips;
    }

    private static void ParseBranchTips(string output, Dictionary<string, string> tips)
    {
        foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = line.IndexOf('|');
            if (separatorIndex <= 0) continue;

            var commitHash = line.Substring(0, separatorIndex).Trim();
            var branchName = line.Substring(separatorIndex + 1).Trim();
            tips[branchName] = commitHash;
        }
    }

    private static Dictionary<string, HashSet<string>> BuildFirstParentChains(
        string directory,
        Dictionary<string, string> branchTips)
    {
        var chains = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in branchTips)
        {
            var branchName = kvp.Key;
            try
            {
                var output = GitCommandRunner.RunCommand($"rev-list --first-parent {branchName}", directory);
                var commits = new HashSet<string>(
                    output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim()));
                chains[branchName] = commits;
            }
            catch
            {
                // Remote-only branches need to be addressed via origin/ prefix
                try
                {
                    var output = GitCommandRunner.RunCommand($"rev-list --first-parent origin/{branchName}", directory);
                    var commits = new HashSet<string>(
                        output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim()));
                    chains[branchName] = commits;
                }
                catch
                {
                    chains[branchName] = new HashSet<string>();
                }
            }
        }

        return chains;
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

    private static Dictionary<string, string> AssignCommitsToBranches(
        List<CommitInfo> commits,
        Dictionary<string, HashSet<string>> firstParentChains)
    {
        var branchesByPriority = firstParentChains.Keys
            .OrderBy(GetBranchPriority)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var assignment = new Dictionary<string, string>();

        foreach (var commit in commits)
        {
            string? bestBranch = null;
            var bestPriority = int.MaxValue;

            foreach (var branch in branchesByPriority)
            {
                if (!firstParentChains[branch].Contains(commit.Hash)) continue;

                var priority = GetBranchPriority(branch);
                if (priority >= bestPriority) continue;

                bestPriority = priority;
                bestBranch = branch;
            }

            assignment[commit.Hash] = bestBranch ?? branchesByPriority.FirstOrDefault() ?? "main";
        }

        return assignment;
    }

    private static Dictionary<string, string> GetCommitSubjects(
        string directory,
        List<CommitInfo> commits)
    {
        var subjects = new Dictionary<string, string>();
        // Only fetch subjects for merge commits to minimize git calls
        var mergeHashes = commits
            .Where(c => c.Parents.Count >= 2)
            .Select(c => c.Hash)
            .ToList();

        if (mergeHashes.Count == 0)
            return subjects;

        foreach (var hash in mergeHashes)
        {
            try
            {
                var subject = GitCommandRunner.RunCommand($"log -1 --format=%s {hash}", directory);
                subjects[hash] = subject;
            }
            catch
            {
                // Skip
            }
        }

        return subjects;
    }

    /// <summary>
    /// Detects commits that are only reachable as second parents of merge
    /// commits and are not on any known branch's first-parent chain.
    /// Creates synthetic branch entries for these orphaned chains so they
    /// can be reconstructed.
    /// </summary>
    private static void DetectOrphanBranches(
        List<CommitInfo> commits,
        Dictionary<string, string> commitBranch,
        Dictionary<string, HashSet<string>> firstParentChains,
        Dictionary<string, string> commitSubjects)
    {
        var allFpCommits = new HashSet<string>();
        foreach (var chain in firstParentChains.Values)
            foreach (var hash in chain)
                allFpCommits.Add(hash);

        var commitLookup = commits.ToDictionary(c => c.Hash);
        var orphanCounter = 0;

        foreach (var mergeCommit in commits.Where(c => c.Parents.Count >= 2))
        {
            var secondParent = mergeCommit.Parents[1];
            if (allFpCommits.Contains(secondParent)) continue;

            // Trace back through first parents to find the full orphan chain
            var orphanChain = new List<string>();
            var current = secondParent;
            while (current != null && !allFpCommits.Contains(current))
            {
                orphanChain.Add(current);
                if (!commitLookup.TryGetValue(current, out var info) || info.Parents.Count == 0)
                    break;
                current = info.Parents[0];
            }

            if (orphanChain.Count == 0) continue;

            // Infer branch name from merge commit message
            var branchName = InferMergeSourceBranch(mergeCommit.Hash, commitSubjects);
            if (branchName == null)
            {
                orphanCounter++;
                branchName = $"merged-branch-{orphanCounter}";
            }

            // Avoid colliding with existing branches
            if (firstParentChains.ContainsKey(branchName))
                continue;

            // Register the orphan chain as a synthetic branch
            var chainSet = new HashSet<string>(orphanChain);
            // Also include any commits the chain shares with known branches
            // (the chain's root's first parent may be on a known branch)
            firstParentChains[branchName] = chainSet;

            // Reassign orphan commits to the synthetic branch
            foreach (var hash in orphanChain)
            {
                commitBranch[hash] = branchName;
                allFpCommits.Add(hash);
            }

            // Check for nested merges within the orphan chain
            foreach (var hash in orphanChain)
            {
                if (!commitLookup.TryGetValue(hash, out var info)) continue;
                if (info.Parents.Count < 2) continue;

                var nestedSecondParent = info.Parents[1];
                if (!allFpCommits.Contains(nestedSecondParent))
                {
                    // Nested orphan — will be handled by a subsequent iteration
                    // since we process merge commits in topo order
                }
            }
        }
    }

    private static readonly Regex MergeBranchPattern = new Regex(
        @"Merge (?:remote-tracking )?branch '(?:origin/)?([^']+)'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static string? InferMergeSourceBranch(
        string mergeHash,
        Dictionary<string, string> commitSubjects)
    {
        if (!commitSubjects.TryGetValue(mergeHash, out var subject))
            return null;

        var match = MergeBranchPattern.Match(subject);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static Dictionary<string, List<string>> BuildTagMap(
        string directory,
        List<CommitInfo> commits)
    {
        var tagMap = new Dictionary<string, List<string>>();
        var allCommitHashes = new HashSet<string>(commits.Select(c => c.Hash));

        try
        {
            var output = GitCommandRunner.RunCommand("tag -l", directory);
            foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var tagName = line.Trim();
                if (!IsVersionTag(tagName)) continue;

                try
                {
                    var tagCommit = GitCommandRunner.RunCommand($"rev-parse \"{tagName}^{{commit}}\"", directory);
                    if (!allCommitHashes.Contains(tagCommit)) continue;

                    if (!tagMap.TryGetValue(tagCommit, out var tags))
                    {
                        tags = new List<string>();
                        tagMap[tagCommit] = tags;
                    }
                    tags.Add(tagName);
                }
                catch
                {
                    // Skip unresolvable tags
                }
            }
        }
        catch
        {
            // No tags
        }

        return tagMap;
    }

    /// <summary>
    /// Produces builder steps using a demand-driven depth-first walk.
    /// At each branch creation point, the child branch is processed eagerly.
    /// Before each merge, the source branch is recursively advanced to exactly
    /// the commit being merged, ensuring the builder's branch state matches
    /// the original topology.
    /// </summary>
    private static List<BuilderStep> ProduceSteps(
        List<CommitInfo> allCommits,
        Dictionary<string, string> commitBranch,
        Dictionary<string, List<string>> commitTags,
        Dictionary<string, string> branchTips,
        string currentBranch)
    {
        // Group commits by branch, preserving topological order within each branch
        var branchCommits = new Dictionary<string, List<CommitInfo>>(StringComparer.OrdinalIgnoreCase);
        foreach (var commit in allCommits)
        {
            var branch = commitBranch[commit.Hash];
            if (!branchCommits.ContainsKey(branch))
                branchCommits[branch] = new List<CommitInfo>();
            branchCommits[branch].Add(commit);
        }

        // Build lookup: commit hash → (branch, index within that branch's list)
        var commitLocation = new Dictionary<string, (string branch, int index)>();
        foreach (var kvp in branchCommits)
        {
            for (var i = 0; i < kvp.Value.Count; i++)
                commitLocation[kvp.Value[i].Hash] = (kvp.Key, i);
        }

        // Determine the root branch (the one that owns the root commit)
        var rootCommit = allCommits.FirstOrDefault(c => c.Parents.Count == 0);
        var rootBranch = rootCommit != null ? commitBranch[rootCommit.Hash] : "main";

        // Pre-compute branch creation points: commit hash → child branches to create
        var branchCreationPoints = new Dictionary<string, List<string>>();
        foreach (var kvp in branchCommits)
        {
            var branch = kvp.Key;
            var commits = kvp.Value;
            if (string.Equals(branch, rootBranch, StringComparison.OrdinalIgnoreCase)) continue;
            if (commits.Count == 0 || commits[0].Parents.Count == 0) continue;

            var branchPointHash = commits[0].Parents[0];
            if (!branchCreationPoints.TryGetValue(branchPointHash, out var list))
            {
                list = new List<string>();
                branchCreationPoints[branchPointHash] = list;
            }
            list.Add(branch);
        }

        foreach (var kvp in branchCreationPoints)
            kvp.Value.Sort((a, b) => GetBranchPriority(a).CompareTo(GetBranchPriority(b)));

        // Per-branch progress: index of the last processed commit (-1 = none)
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
            if (string.Equals(builderBranch, targetBranch, StringComparison.OrdinalIgnoreCase)) return;
            FlushPending();
            steps.Add(BuilderStep.Checkout(targetBranch));
            builderBranch = targetBranch;
        }

        void ProcessBranchTo(string branch, int targetIndex)
        {
            if (!branchCommits.TryGetValue(branch, out var commits)) return;
            if (targetIndex >= commits.Count)
                targetIndex = commits.Count - 1;

            while (processedIndex[branch] < targetIndex)
            {
                var i = processedIndex[branch] + 1;
                processedIndex[branch] = i;
                var commit = commits[i];

                if (commit.Parents.Count == 0)
                {
                    steps.Add(BuilderStep.InitialCommit());
                    builderBranch = branch;
                }
                else if (commit.Parents.Count >= 2)
                {
                    // Before merging, advance the source branch to the merge point
                    var secondParent = commit.Parents[1];
                    if (commitLocation.TryGetValue(secondParent, out var sourceLoc))
                    {
                        if (processedIndex.TryGetValue(sourceLoc.branch, out var sourceProgress)
                            && sourceProgress < sourceLoc.index)
                        {
                            ProcessBranchTo(sourceLoc.branch, sourceLoc.index);
                        }
                    }

                    EnsureOnBranch(branch);
                    FlushPending();
                    var sourceBranch = commitBranch.TryGetValue(secondParent, out var sb) ? sb : "unknown";
                    steps.Add(BuilderStep.Merge(sourceBranch));
                }
                else
                {
                    EnsureOnBranch(branch);
                    pendingCommits++;
                }

                // Tags
                if (commitTags.TryGetValue(commit.Hash, out var tags))
                {
                    FlushPending();
                    foreach (var tag in tags)
                        steps.Add(BuilderStep.Tag(tag));
                }

                // At branch creation points, eagerly process child branches
                if (branchCreationPoints.TryGetValue(commit.Hash, out var children))
                {
                    FlushPending();
                    foreach (var child in children)
                    {
                        if (createdBranches.Contains(child)) continue;
                        EnsureOnBranch(branch);
                        steps.Add(BuilderStep.Branch(child));
                        createdBranches.Add(child);
                        builderBranch = child;

                        if (branchCommits.TryGetValue(child, out var childCommits))
                            ProcessBranchTo(child, childCommits.Count - 1);
                    }
                }
            }

            FlushPending();
        }

        // Process root branch (depth-first recursion handles all reachable branches)
        if (branchCommits.TryGetValue(rootBranch, out var rootCommits))
            ProcessBranchTo(rootBranch, rootCommits.Count - 1);

        // Process any branches not reached by the depth-first walk
        foreach (var branch in branchCommits.Keys.OrderBy(GetBranchPriority))
        {
            if (!branchCommits.TryGetValue(branch, out var bc)) continue;
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
