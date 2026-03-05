namespace Timtek.GitFlowVersioning.Versioning;

/// <summary>Contains all computed version variables for a build.</summary>
public sealed class VersionInfo
{
    /// <summary>Gets or sets the major version number as a string.</summary>
    public string Major { get; set; } = "0";

    /// <summary>Gets or sets the minor version number as a string.</summary>
    public string Minor { get; set; } = "0";

    /// <summary>Gets or sets the patch version number as a string.</summary>
    public string Patch { get; set; } = "0";

    /// <summary>Gets or sets the "Major.Minor.Patch" string.</summary>
    public string MajorMinorPatch { get; set; } = "0.0.0";

    /// <summary>Gets or sets the prerelease label ("alpha", "beta", or "").</summary>
    public string PreReleaseLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the prerelease label with leading dash ("-alpha", "-beta", or "").</summary>
    public string PreReleaseLabelWithDash { get; set; } = string.Empty;

    /// <summary>Gets or sets the prerelease number (commit distance) as a string.</summary>
    public string PreReleaseNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the full prerelease tag ("alpha.5", "beta.5", or "").</summary>
    public string PreReleaseTag { get; set; } = string.Empty;

    /// <summary>Gets or sets the full prerelease tag with leading dash ("-alpha.5", "-beta.5", or "").</summary>
    public string PreReleaseTagWithDash { get; set; } = string.Empty;

    /// <summary>Gets or sets the SemVer string ("M.N.P" or "M.N.P-alpha.5").</summary>
    public string SemVer { get; set; } = "0.0.0";

    /// <summary>Gets or sets the full SemVer string (SemVer with optional build metadata).</summary>
    public string FullSemVer { get; set; } = "0.0.0";

    /// <summary>Gets or sets the full branch name.</summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>Gets or sets the branch name with '/' replaced by '-'.</summary>
    public string EscapedBranchName { get; set; } = string.Empty;

    /// <summary>Gets or sets the full commit SHA.</summary>
    public string Sha { get; set; } = string.Empty;

    /// <summary>Gets or sets the first 7 characters of the commit SHA.</summary>
    public string ShortSha { get; set; } = string.Empty;

    /// <summary>Gets or sets the commit distance as a string.</summary>
    public string BuildMetaData { get; set; } = string.Empty;

    /// <summary>Gets or sets the full build metadata string.</summary>
    public string FullBuildMetaData { get; set; } = string.Empty;

    /// <summary>Gets or sets the informational version (SemVer+FullBuildMetaData).</summary>
    public string InformationalVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the assembly semantic version ("M.N.0.0").</summary>
    public string AssemblySemVer { get; set; } = "0.0.0.0";

    /// <summary>Gets or sets the assembly file version ("M.N.P.0").</summary>
    public string AssemblySemFileVer { get; set; } = "0.0.0.0";

    /// <summary>Returns the <see cref="SemVer"/> value.</summary>
    public override string ToString() => SemVer;
}
