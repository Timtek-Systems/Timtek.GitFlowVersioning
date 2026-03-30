using Microsoft.Build.Framework;
using Timtek.GitFlowVersion.CI;
using Timtek.GitFlowVersion.Generation;
using Timtek.GitFlowVersion.Git;
using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.Tasks;

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
            CiMessageEmitter.Emit(versionInfo, message => Log.LogMessage(MessageImportance.High, message));
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Timtek.GitFlowVersion: Failed to compute version: {ex.Message}. Falling back to '{UnversionedVersion}'.");
            SetFallbackVersion();
        }

        return true;
    }

    private GitCommitInfo GatherGitInfo() => GitInfoGatherer.Gather(ProjectDirectory);

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

        var assemblyInfoFile = Path.Combine(IntermediateOutputPath, "Timtek.GitFlowVersion.AssemblyInfo.g.cs");
        File.WriteAllText(assemblyInfoFile, GitVersionInformationGenerator.GenerateAssemblyInfo(versionInfo));
        AssemblyInfoFile = assemblyInfoFile;
    }

    private void LogVersionSummary(VersionInfo v)
    {
        Log.LogMessage(MessageImportance.High, "");
        Log.LogMessage(MessageImportance.High, "Timtek.GitFlowVersion:");
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
