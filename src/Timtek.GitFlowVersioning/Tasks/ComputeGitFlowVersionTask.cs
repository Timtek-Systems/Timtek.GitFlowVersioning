using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Timtek.GitFlowVersioning.CI;
using Timtek.GitFlowVersioning.Generation;
using Timtek.GitFlowVersioning.Git;
using Timtek.GitFlowVersioning.Versioning;

namespace Timtek.GitFlowVersioning.Tasks;

/// <summary>MSBuild task that computes GitFlow-based semantic versions from git history.</summary>
public class ComputeGitFlowVersionTask : Microsoft.Build.Utilities.Task
{
    private const string UnversionedVersion = "0.0.0-unversioned";

    /// <summary>Gets or sets the project directory (MSBuild's $(MSBuildProjectDirectory)).</summary>
    [Required]
    public string ProjectDirectory { get; set; } = string.Empty;

    /// <summary>Gets or sets the intermediate output path where generated files will be written.</summary>
    [Required]
    public string IntermediateOutputPath { get; set; } = string.Empty;

    // Output properties - version variables
    [Output] public string Major { get; private set; } = "0";
    [Output] public string Minor { get; private set; } = "0";
    [Output] public string Patch { get; private set; } = "0";
    [Output] public string MajorMinorPatch { get; private set; } = "0.0.0";
    [Output] public string PreReleaseLabel { get; private set; } = string.Empty;
    [Output] public string PreReleaseLabelWithDash { get; private set; } = string.Empty;
    [Output] public string PreReleaseNumber { get; private set; } = string.Empty;
    [Output] public string PreReleaseTag { get; private set; } = string.Empty;
    [Output] public string PreReleaseTagWithDash { get; private set; } = string.Empty;
    [Output] public string SemVer { get; private set; } = UnversionedVersion;
    [Output] public string FullSemVer { get; private set; } = UnversionedVersion;
    [Output] public string BranchName { get; private set; } = string.Empty;
    [Output] public string EscapedBranchName { get; private set; } = string.Empty;
    [Output] public string Sha { get; private set; } = string.Empty;
    [Output] public string ShortSha { get; private set; } = string.Empty;
    [Output] public string BuildMetaData { get; private set; } = string.Empty;
    [Output] public string FullBuildMetaData { get; private set; } = string.Empty;
    [Output] public string InformationalVersion { get; private set; } = UnversionedVersion;
    [Output] public string AssemblySemVer { get; private set; } = "0.0.0.0";
    [Output] public string AssemblySemFileVer { get; private set; } = "0.0.0.0";
    [Output] public string GitVersionInformationFile { get; private set; } = string.Empty;
    [Output] public string AssemblyInfoFile { get; private set; } = string.Empty;

    /// <inheritdoc />
    public override bool Execute()
    {
        try
        {
            var commitInfo = GatherGitInfo();
            var versionInfo = VersionCalculator.Calculate(commitInfo);
            ApplyVersionInfo(versionInfo);
            WriteGeneratedFiles(versionInfo);
            LogVersionSummary(versionInfo);
            CiMessageEmitter.Emit(versionInfo);
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Timtek.GitFlowVersioning: Failed to compute version: {ex.Message}. Falling back to '{UnversionedVersion}'.");
            SetFallbackVersion();
        }

        return true;
    }

    private GitCommitInfo GatherGitInfo()
    {
        var repoRoot = FindRepoRoot();
        var sha = GitCommandRunner.RunCommand("rev-parse HEAD", repoRoot);
        var branchName = GitCommandRunner.RunCommand("rev-parse --abbrev-ref HEAD", repoRoot);

        if (branchName == "HEAD")
            branchName = TryGetBranchNameFromDetachedHead(repoRoot, sha);

        var (baseTag, distance, hasTag) = GetVersionFromDescribe(repoRoot);

        return new GitCommitInfo
        {
            Sha = sha,
            BranchName = branchName,
            BaseVersionTag = baseTag,
            CommitDistance = distance,
            HasTag = hasTag
        };
    }

    private string FindRepoRoot()
    {
        try
        {
            return GitCommandRunner.RunCommand("rev-parse --show-toplevel", ProjectDirectory);
        }
        catch
        {
            return ProjectDirectory;
        }
    }

    private static string TryGetBranchNameFromDetachedHead(string repoRoot, string sha)
    {
        try
        {
            var result = GitCommandRunner.RunCommand($"name-rev --name-only {sha}", repoRoot);
            return result.Replace("remotes/origin/", "").Trim();
        }
        catch
        {
            return "HEAD";
        }
    }

    private static (string baseTag, int distance, bool hasTag) GetVersionFromDescribe(string repoRoot)
    {
        string describeOutput;
        try
        {
            describeOutput = GitCommandRunner.RunCommand("describe --tags --long --match \"v*.*.*\" HEAD", repoRoot);
        }
        catch
        {
            try
            {
                describeOutput = GitCommandRunner.RunCommand("describe --tags --long --match \"*.*.*\" HEAD", repoRoot);
            }
            catch
            {
                return GetFallbackFromCommitCount(repoRoot);
            }
        }

        return ParseDescribeOutput(describeOutput);
    }

    private static (string baseTag, int distance, bool hasTag) GetFallbackFromCommitCount(string repoRoot)
    {
        try
        {
            var countStr = GitCommandRunner.RunCommand("rev-list --count HEAD", repoRoot);
            var count = int.TryParse(countStr, out var c) ? c : 0;
            return ("0.1.0", count, false);
        }
        catch
        {
            return ("0.1.0", 0, false);
        }
    }

    private static (string baseTag, int distance, bool hasTag) ParseDescribeOutput(string describeOutput)
    {
        // format: "v1.2.3-5-gabcdef" or "1.2.3-5-gabcdef"
        var lastDash = describeOutput.LastIndexOf('-');
        if (lastDash < 0) return ("0.1.0", 0, true);

        var secondLastDash = describeOutput.LastIndexOf('-', lastDash - 1);
        if (secondLastDash < 0) return ("0.1.0", 0, true);

        var tagPart = describeOutput.Substring(0, secondLastDash).TrimStart('v', 'V');
        var distancePart = describeOutput.Substring(secondLastDash + 1, lastDash - secondLastDash - 1);

        var distance = int.TryParse(distancePart, out var d) ? d : 0;
        return (tagPart, distance, true);
    }

    private void ApplyVersionInfo(VersionInfo v)
    {
        Major = v.Major;
        Minor = v.Minor;
        Patch = v.Patch;
        MajorMinorPatch = v.MajorMinorPatch;
        PreReleaseLabel = v.PreReleaseLabel;
        PreReleaseLabelWithDash = v.PreReleaseLabelWithDash;
        PreReleaseNumber = v.PreReleaseNumber;
        PreReleaseTag = v.PreReleaseTag;
        PreReleaseTagWithDash = v.PreReleaseTagWithDash;
        SemVer = v.SemVer;
        FullSemVer = v.FullSemVer;
        BranchName = v.BranchName;
        EscapedBranchName = v.EscapedBranchName;
        Sha = v.Sha;
        ShortSha = v.ShortSha;
        BuildMetaData = v.BuildMetaData;
        FullBuildMetaData = v.FullBuildMetaData;
        InformationalVersion = v.InformationalVersion;
        AssemblySemVer = v.AssemblySemVer;
        AssemblySemFileVer = v.AssemblySemFileVer;
    }

    private void WriteGeneratedFiles(VersionInfo versionInfo)
    {
        Directory.CreateDirectory(IntermediateOutputPath);

        var gitVersionFile = Path.Combine(IntermediateOutputPath, "GitVersionInformation.g.cs");
        File.WriteAllText(gitVersionFile, GitVersionInformationGenerator.Generate(versionInfo));
        GitVersionInformationFile = gitVersionFile;

        var assemblyInfoFile = Path.Combine(IntermediateOutputPath, "Timtek.GitFlowVersioning.AssemblyInfo.g.cs");
        File.WriteAllText(assemblyInfoFile, GitVersionInformationGenerator.GenerateAssemblyInfo(versionInfo));
        AssemblyInfoFile = assemblyInfoFile;
    }

    private void LogVersionSummary(VersionInfo v)
    {
        Log.LogMessage(MessageImportance.High, "");
        Log.LogMessage(MessageImportance.High, "Timtek.GitFlowVersioning:");
        Log.LogMessage(MessageImportance.High, $"  FullSemVer:           {v.FullSemVer}");
        Log.LogMessage(MessageImportance.High, $"  SemVer:               {v.SemVer}");
        Log.LogMessage(MessageImportance.High, $"  AssemblySemVer:       {v.AssemblySemVer}");
        Log.LogMessage(MessageImportance.High, $"  AssemblySemFileVer:   {v.AssemblySemFileVer}");
        Log.LogMessage(MessageImportance.High, $"  InformationalVersion: {v.InformationalVersion}");
        Log.LogMessage(MessageImportance.High, $"  BranchName:           {v.BranchName}");
        Log.LogMessage(MessageImportance.High, $"  Sha:                  {v.Sha}");
        Log.LogMessage(MessageImportance.High, "");
    }

    private void SetFallbackVersion()
    {
        SemVer = UnversionedVersion;
        FullSemVer = UnversionedVersion;
        InformationalVersion = UnversionedVersion;
        AssemblySemVer = "0.0.0.0";
        AssemblySemFileVer = "0.0.0.0";
    }
}
