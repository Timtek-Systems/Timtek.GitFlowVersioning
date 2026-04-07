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
        var taggedPrerelease = ParseTaggedPrerelease(commitInfo.BaseVersionTag);

        return branchType switch
        {
            BranchType.Main => BuildMainVersion(baseVersion, distance, commitInfo),
            BranchType.Release when IsExactTaggedCommit(commitInfo) && taggedPrerelease is { } releaseTag
                => BuildPrereleaseVersion(baseVersion, distance, releaseTag.label, commitInfo, releaseTag.number),
            BranchType.Release => BuildBranchPrereleaseVersion(baseVersion, distance, "beta", taggedPrerelease, commitInfo),
            BranchType.Hotfix when IsExactTaggedCommit(commitInfo) && taggedPrerelease is { } hotfixTag
                => BuildPrereleaseVersion(baseVersion, distance, hotfixTag.label, commitInfo, hotfixTag.number),
            BranchType.Hotfix => BuildBranchPrereleaseVersion(baseVersion, distance, "beta", taggedPrerelease, commitInfo),
            BranchType.Develop => BuildDevelopVersion(baseVersion, distance, commitInfo),
            _ when IsExactTaggedCommit(commitInfo) && taggedPrerelease is { } otherTag
                => BuildPrereleaseVersion(baseVersion, distance, otherTag.label, commitInfo, otherTag.number),
            _ when IsExactTaggedCommit(commitInfo)
                => BuildMainVersion(baseVersion, distance, commitInfo),
            _ => BuildPrereleaseVersion(baseVersion, distance, "alpha", commitInfo),
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

    private static VersionInfo BuildBranchPrereleaseVersion(Version baseVersion, int distance, string label, (string label, string number)? taggedPrerelease, GitCommitInfo commitInfo)
    {
        var preReleaseNumber = ResolveBranchPrereleaseNumber(label, taggedPrerelease, distance);
        return BuildPrereleaseVersion(baseVersion, distance, label, commitInfo, preReleaseNumber);
    }

    private static string ResolveBranchPrereleaseNumber(string label, (string label, string number)? taggedPrerelease, int distance)
    {
        if (taggedPrerelease is not { } parsedTag || !string.Equals(parsedTag.label, label, StringComparison.OrdinalIgnoreCase))
            return distance.ToString();

        return (int.Parse(parsedTag.number) + distance).ToString();
    }

    private static VersionInfo BuildPrereleaseVersion(Version baseVersion, int distance, string label, GitCommitInfo commitInfo, string? preReleaseNumberOverride = null)
    {
        var major = baseVersion.Major.ToString();
        var minor = baseVersion.Minor.ToString();
        var patch = baseVersion.Build.ToString();
        var mmp = $"{major}.{minor}.{patch}";
        var preReleaseNumber = preReleaseNumberOverride ?? distance.ToString();
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

    private static (string label, string number)? ParseTaggedPrerelease(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        var clean = tag.TrimStart('v', 'V');
        var hyphenIndex = clean.IndexOf('-');
        if (hyphenIndex <= 0 || hyphenIndex == clean.Length - 1)
            return null;

        var prereleasePart = clean.Substring(hyphenIndex + 1);
        var parts = prereleasePart.Split('.');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            return null;

        return int.TryParse(parts[1], out _) ? (parts[0], parts[1]) : null;
    }

    private static Version ParseBaseVersion(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return FallbackVersion;

        var clean = tag.TrimStart('v', 'V');

        // Strip any prerelease suffix (e.g. "-beta.12") before parsing so that
        // tags like "2.0.0-beta.12" yield a base of 2.0.0 rather than falling back.
        var hyphenIndex = clean.IndexOf('-');
        if (hyphenIndex > 0)
            clean = clean.Substring(0, hyphenIndex);

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
