using Timtek.GitFlowVersion.Git;
using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

/// <summary>
/// Builds a temporary Git repository and computes version information from it.
/// Mirrors the <see cref="GitTestRepoBuilder"/> fluent API and adds a
/// <see cref="Build"/> method that returns a <see cref="VersionComputationContext"/>.
/// </summary>
internal sealed class VersionComputationBuilder : IDisposable
{
    private readonly GitTestRepoBuilder repo = new();

    /// <summary>Gets the root path of the temporary Git repository.</summary>
    public string RepoPath => repo.RepoPath;

    /// <summary>Creates an initial commit on the current branch.</summary>
    public VersionComputationBuilder WithInitialCommit()
    {
        repo.WithInitialCommit();
        return this;
    }

    /// <summary>Creates a lightweight tag at the current HEAD.</summary>
    public VersionComputationBuilder WithTag(string tagName)
    {
        repo.WithTag(tagName);
        return this;
    }

    /// <summary>Creates the specified number of commits on the current branch.</summary>
    public VersionComputationBuilder WithCommits(int count, string messagePrefix = "Commit")
    {
        repo.WithCommits(count, messagePrefix);
        return this;
    }

    /// <summary>Creates a new branch from the specified ref and checks it out.</summary>
    public VersionComputationBuilder WithBranch(string branchName, string fromRef = "HEAD")
    {
        repo.WithBranch(branchName, fromRef);
        return this;
    }

    /// <summary>Checks out an existing branch.</summary>
    public VersionComputationBuilder OnBranch(string branchName)
    {
        repo.OnBranch(branchName);
        return this;
    }

    /// <summary>Merges the specified branch into the current branch using a merge commit.</summary>
    public VersionComputationBuilder MergeFrom(string sourceBranch)
    {
        repo.MergeFrom(sourceBranch);
        return this;
    }

    /// <summary>
    /// Gathers commit information from the constructed repository, computes version
    /// variables, and returns the result as a <see cref="VersionComputationContext"/>.
    /// </summary>
    public VersionComputationContext Build()
    {
        var commitInfo = GitInfoGatherer.Gather(repo.RepoPath);
        var result = VersionCalculator.Calculate(commitInfo);
        return new VersionComputationContext(result);
    }

    /// <inheritdoc />
    public void Dispose() => repo.Dispose();
}
