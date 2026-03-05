namespace Timtek.GitFlowVersioning.Versioning;

/// <summary>Computes <see cref="VersionInfo"/> from raw <see cref="GitCommitInfo"/>.</summary>
public static class VersionCalculator
{
    private static readonly Version FallbackVersion = new Version("0.1.0");

    /// <summary>Calculates all version variables from the provided <paramref name="commitInfo"/>.</summary>
    /// <param name="commitInfo">The raw git commit information.</param>
    /// <returns>A fully populated <see cref="VersionInfo"/>.</returns>
    public static VersionInfo Calculate(GitCommitInfo commitInfo)
    {
        var baseVersion = ParseBaseVersion(commitInfo.BaseVersionTag);
        var branchType = BranchClassifier.Classify(commitInfo.BranchName);
        var distance = commitInfo.CommitDistance;

        return branchType switch
        {
            BranchType.Main    => BuildMainVersion(baseVersion, distance, commitInfo),
            BranchType.Release => BuildPrereleaseVersion(baseVersion, distance, "beta", commitInfo),
            BranchType.Hotfix  => BuildPrereleaseVersion(baseVersion, distance, "beta", commitInfo),
            BranchType.Develop => BuildDevelopVersion(baseVersion, distance, commitInfo),
            _                  => BuildPrereleaseVersion(baseVersion, distance, "alpha", commitInfo),
        };
    }

    private static VersionInfo BuildMainVersion(Version baseVersion, int distance, GitCommitInfo commitInfo)
    {
        var patch = baseVersion.Build + distance;
        var major = baseVersion.Major.ToString();
        var minor = baseVersion.Minor.ToString();
        var patchStr = patch.ToString();
        var mmp = $"{major}.{minor}.{patchStr}";
        var sha = commitInfo.Sha;
        var shortSha = TruncateSha(sha);
        var branchName = commitInfo.BranchName;
        var escapedBranchName = EscapeBranchName(branchName);
        var buildMetaData = distance.ToString();
        var fullBuildMetaData = $"{buildMetaData}.Branch.{branchName}.Sha.{sha}";
        var informationalVersion = $"{mmp}+{fullBuildMetaData}";

        return new VersionInfo
        {
            Major = major,
            Minor = minor,
            Patch = patchStr,
            MajorMinorPatch = mmp,
            PreReleaseLabel = string.Empty,
            PreReleaseLabelWithDash = string.Empty,
            PreReleaseNumber = string.Empty,
            PreReleaseTag = string.Empty,
            PreReleaseTagWithDash = string.Empty,
            SemVer = mmp,
            FullSemVer = mmp,
            BranchName = branchName,
            EscapedBranchName = escapedBranchName,
            Sha = sha,
            ShortSha = shortSha,
            BuildMetaData = buildMetaData,
            FullBuildMetaData = fullBuildMetaData,
            InformationalVersion = informationalVersion,
            AssemblySemVer = $"{major}.{minor}.0.0",
            AssemblySemFileVer = $"{major}.{minor}.{patchStr}.0"
        };
    }

    private static VersionInfo BuildPrereleaseVersion(Version baseVersion, int distance, string label, GitCommitInfo commitInfo)
    {
        var major = baseVersion.Major.ToString();
        var minor = baseVersion.Minor.ToString();
        var patch = baseVersion.Build.ToString();
        var mmp = $"{major}.{minor}.{patch}";
        var preReleaseNumber = distance.ToString();
        var preReleaseTag = $"{label}.{preReleaseNumber}";
        var preReleaseTagWithDash = $"-{preReleaseTag}";
        var semVer = $"{mmp}{preReleaseTagWithDash}";
        var sha = commitInfo.Sha;
        var shortSha = TruncateSha(sha);
        var branchName = commitInfo.BranchName;
        var escapedBranchName = EscapeBranchName(branchName);
        var buildMetaData = distance.ToString();
        var fullBuildMetaData = $"{buildMetaData}.Branch.{branchName}.Sha.{sha}";
        var fullSemVer = $"{semVer}+{buildMetaData}";
        var informationalVersion = $"{semVer}+{fullBuildMetaData}";

        return new VersionInfo
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            MajorMinorPatch = mmp,
            PreReleaseLabel = label,
            PreReleaseLabelWithDash = $"-{label}",
            PreReleaseNumber = preReleaseNumber,
            PreReleaseTag = preReleaseTag,
            PreReleaseTagWithDash = preReleaseTagWithDash,
            SemVer = semVer,
            FullSemVer = fullSemVer,
            BranchName = branchName,
            EscapedBranchName = escapedBranchName,
            Sha = sha,
            ShortSha = shortSha,
            BuildMetaData = buildMetaData,
            FullBuildMetaData = fullBuildMetaData,
            InformationalVersion = informationalVersion,
            AssemblySemVer = $"{major}.{minor}.0.0",
            AssemblySemFileVer = $"{major}.{minor}.{patch}.0"
        };
    }

    private static VersionInfo BuildDevelopVersion(Version baseVersion, int distance, GitCommitInfo commitInfo)
    {
        var developBase = new Version(baseVersion.Major, baseVersion.Minor + 1, 0);
        return BuildPrereleaseVersion(developBase, distance, "alpha", commitInfo);
    }

    private static string TruncateSha(string sha) =>
        sha.Length >= 7 ? sha.Substring(0, 7) : sha;

    private static string EscapeBranchName(string branchName) =>
        branchName.Replace("/", "-");

    private static Version ParseBaseVersion(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return FallbackVersion;

        var clean = tag.TrimStart('v', 'V');
        Version? v;
        if (Version.TryParse(clean, out v))
            return new Version(v.Major, v.Minor, v.Build < 0 ? 0 : v.Build);
        return FallbackVersion;
    }
}
