using Timtek.GitFlowVersion.Git;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

/// <summary>
/// Builds isolated temporary Git repositories for integration testing.
/// Each instance creates a fresh repo in the system temp directory that is
/// cleaned up on disposal.
/// </summary>
internal sealed class GitTestRepoBuilder : IDisposable
{
    private readonly string repoPath;

    public GitTestRepoBuilder()
    {
        repoPath = Path.Combine(Path.GetTempPath(), $"gfv-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(repoPath);
        RunGit("init");
        RunGit("config user.name \"Test\"");
        RunGit("config user.email \"test@test.com\"");
    }

    /// <summary>Gets the root path of the temporary Git repository.</summary>
    public string RepoPath => repoPath;

    /// <summary>Creates an initial commit on the current branch.</summary>
    public GitTestRepoBuilder WithInitialCommit()
    {
        File.WriteAllText(Path.Combine(repoPath, "README.md"), "# Test");
        RunGit("add .");
        RunGit("commit -m \"Initial commit\"");
        return this;
    }

    /// <summary>Creates a lightweight tag at the current HEAD.</summary>
    public GitTestRepoBuilder WithTag(string tagName)
    {
        RunGit($"tag {tagName}");
        return this;
    }

    /// <summary>Creates the specified number of commits on the current branch.</summary>
    public GitTestRepoBuilder WithCommits(int count, string messagePrefix = "Commit")
    {
        for (var i = 1; i <= count; i++)
        {
            var fileName = $"{messagePrefix.Replace(" ", "-")}-{i}-{Guid.NewGuid():N}.txt";
            File.WriteAllText(Path.Combine(repoPath, fileName), $"Content {i}");
            RunGit("add .");
            RunGit($"commit -m \"{messagePrefix} {i}\"");
        }
        return this;
    }

    /// <summary>Creates a new branch from the specified ref and checks it out.</summary>
    public GitTestRepoBuilder WithBranch(string branchName, string fromRef = "HEAD")
    {
        RunGit($"checkout -b {branchName} {fromRef}");
        return this;
    }

    /// <summary>Checks out an existing branch.</summary>
    public GitTestRepoBuilder OnBranch(string branchName)
    {
        RunGit($"checkout {branchName}");
        return this;
    }

    /// <summary>Merges the specified branch into the current branch using a merge commit.</summary>
    public GitTestRepoBuilder MergeFrom(string sourceBranch)
    {
        RunGit($"merge --no-ff {sourceBranch} -m \"Merge {sourceBranch}\"");
        return this;
    }

    private void RunGit(string arguments)
    {
        GitCommandRunner.RunCommand(arguments, repoPath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(repoPath))
            {
                // Git marks some objects as read-only; remove that attribute before deleting
                foreach (var file in new DirectoryInfo(repoPath).GetFiles("*", SearchOption.AllDirectories))
                    file.Attributes = FileAttributes.Normal;

                Directory.Delete(repoPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; the OS will reclaim temp space eventually
        }
    }
}
