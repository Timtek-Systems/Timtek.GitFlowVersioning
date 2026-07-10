namespace Timtek.GitFlowVersion.Versioning;

/// <summary>Holds raw information gathered from the git repository for the current commit.</summary>
public sealed class GitCommitInfo
{
    /// <summary>Gets or sets the full commit SHA.</summary>
    public string Sha { get; set; } = string.Empty;

    /// <summary>Gets or sets the full branch name (e.g. "feature/foo").</summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>Gets or sets the base version tag found by git describe (e.g. "1.2.3").</summary>
    public string BaseVersionTag { get; set; } = "0.1.0";

    /// <summary>Gets or sets the number of commits since the base version tag.</summary>
    public int CommitDistance { get; set; }

    /// <summary>Gets or sets a value indicating whether any version tag was found.</summary>
    public bool HasTag { get; set; }

    /// <summary>
    /// Gets or sets a valid prerelease-labeled SemVer tag (e.g. "6.3.0-rc.1") that points exactly at the
    /// current commit, or empty if none exists. This is independent of <see cref="BaseVersionTag"/> and is
    /// used only to support the release-branch tag override mechanism in <c>VersionCalculator</c>.
    /// </summary>
    public string ExactPrereleaseTag { get; set; } = string.Empty;
}
