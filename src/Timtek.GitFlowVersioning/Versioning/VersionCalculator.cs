namespace Timtek.GitFlowVersion.Versioning;

/// <summary>Computes <see cref="VersionInfo"/> from raw <see cref="GitCommitInfo"/>.</summary>
public static class VersionCalculator
{
    private static readonly Version FallbackVersion = new Version("0.1.0");

    /// <summary>Calculates all version variables from the provided <paramref name="commitInfo"/>.</summary>
    /// <param name="commitInfo">The raw git commit information.</param>
    /// <returns>A fully populated <see cref="VersionInfo"/>.</returns>
    public static VersionInfo Calculate(GitCommitInfo commitInfo)
    {
        var parsedBaseVersion = ParseBaseVersion(commitInfo.BaseVersionTag);
        var branchType = BranchClassifier.Classify(commitInfo.BranchName);
        var baseVersion = ResolveBaseVersion(parsedBaseVersion, branchType, commitInfo.BranchName);
        var distance = commitInfo.CommitDistance;

        return branchType switch
        {
            BranchType.Main    => BuildMainVersion(baseVersion, distance, commitInfo),
            BranchType.Release => BuildPrereleaseVersion(baseVersion, distance, "beta", branchType, commitInfo),
            BranchType.Hotfix  => BuildPrereleaseVersion(baseVersion, distance, "beta", branchType, commitInfo),
            BranchType.Develop => BuildDevelopVersion(baseVersion, distance, commitInfo),
            _ when IsExactTaggedCommit(commitInfo)
                               => BuildMainVersion(baseVersion, distance, commitInfo),
            _                  => BuildPrereleaseVersion(baseVersion, distance, "alpha", branchType, commitInfo),
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
            AssemblySemVer = BuildAssemblyVersion(major, minor, patchStr, 0),
            AssemblySemFileVer = BuildAssemblyVersion(major, minor, patchStr, 0)
        };
    }

    private static VersionInfo BuildPrereleaseVersion(Version baseVersion, int distance, string label, BranchType branchType, GitCommitInfo commitInfo)
    {
        var major = baseVersion.Major.ToString();
        var minor = baseVersion.Minor.ToString();
        var patch = baseVersion.Build.ToString();
        var mmp = $"{major}.{minor}.{patch}";
        var preReleaseNumber = distance.ToString();
        var weightedPreReleaseNumber = GetWeightedPreReleaseNumber(branchType, distance);
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
            AssemblySemVer = BuildAssemblyVersion(major, minor, patch, weightedPreReleaseNumber),
            AssemblySemFileVer = BuildAssemblyVersion(major, minor, patch, weightedPreReleaseNumber)
        };
    }

    private static VersionInfo BuildDevelopVersion(Version baseVersion, int distance, GitCommitInfo commitInfo)
    {
        var developBase = new Version(baseVersion.Major, baseVersion.Minor + 1, 0);
        return BuildPrereleaseVersion(developBase, distance, "alpha", BranchType.Develop, commitInfo);
    }

    private static string BuildAssemblyVersion(string major, string minor, string patch, int revision) =>
        $"{major}.{minor}.{patch}.{revision}";

    private static int GetWeightedPreReleaseNumber(BranchType branchType, int preReleaseNumber) =>
        PreReleaseWeightCalculator.GetWeight(branchType) + preReleaseNumber;

    private static string TruncateSha(string sha) =>
        sha.Length >= 7 ? sha.Substring(0, 7) : sha;

    private static string EscapeBranchName(string branchName) =>
        branchName.Replace("/", "-");

    private static Version ResolveBaseVersion(Version tagBaseVersion, BranchType branchType, string branchName)
    {
        if (branchType is not (BranchType.Release or BranchType.Hotfix))
            return tagBaseVersion;

        var slashIndex = branchName.IndexOf('/');
        if (slashIndex < 0 || slashIndex == branchName.Length - 1)
            return tagBaseVersion;

        var versionText = branchName.Substring(slashIndex + 1).Trim();
        var detachedSuffixIndex = versionText.IndexOfAny(new[] { '~', '^' });
        if (detachedSuffixIndex > 0)
            versionText = versionText.Substring(0, detachedSuffixIndex);

        if (!Version.TryParse(versionText, out var parsedBranchVersion))
            return tagBaseVersion;

        return new Version(parsedBranchVersion.Major, parsedBranchVersion.Minor, parsedBranchVersion.Build < 0 ? 0 : parsedBranchVersion.Build);
    }

    private static bool IsExactTaggedCommit(GitCommitInfo commitInfo) =>
        commitInfo.HasTag && commitInfo.CommitDistance == 0;

    private static Version ParseBaseVersion(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return FallbackVersion;

        var clean = tag.TrimStart('v', 'V');
        try
        {
            var parsedVersion = Version.Parse(clean);
            return new Version(parsedVersion.Major, parsedVersion.Minor, parsedVersion.Build < 0 ? 0 : parsedVersion.Build);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
        {
            return FallbackVersion;
        }
    }
}
