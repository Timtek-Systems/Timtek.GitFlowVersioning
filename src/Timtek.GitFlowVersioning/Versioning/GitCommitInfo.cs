namespace Timtek.GitFlowVersioning.Versioning;

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
}
