using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

/// <summary>
/// One-off fixture that builds a sample repository from the captured builder
/// chain and copies it to the Downloads directory for manual examination.
/// Delete this file after use.
/// </summary>
[Subject("Sample repository builder")]/*[Ignore("One-off fixture for manual examination")]*/
class when_building_a_sample_repository_for_examination : With_end_to_end_version_computation
{
    static string DestinationPath = null!;

    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithCommits(1)
        .WithBranch("master")
        .OnBranch("main")
        .WithCommits(8)
        .WithBranch("develop")
        .OnBranch("main")
        .WithCommits(9)
        .WithTag("v0.2.0")
        .OnBranch("develop")
        .MergeFrom("main")
        .WithBranch("release/1.0.1")
        .WithCommits(2)
        .OnBranch("master")
        .WithCommits(1)
        .WithTag("0.1.0")
        .WithCommits(1)
        .MergeFrom("main")
        .WithCommits(2)
        .WithTag("0.1.2")
        .WithCommits(1)
        .OnBranch("release/1.0.1")
        .MergeFrom("master")
        .Build();

    Because of = () =>
    {
        DestinationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "builder-release-1.0.1");

        if (Directory.Exists(DestinationPath))
        {
            foreach (var file in new DirectoryInfo(DestinationPath).GetFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            Directory.Delete(DestinationPath, recursive: true);
        }

        CopyDirectory(Builder.RepoPath, DestinationPath);
    };

    It should_produce_expected_semver = () => Context.Result.SemVer.ShouldEqual("1.0.1-beta.9");
    It should_have_copied_the_repo = () => Directory.Exists(DestinationPath).ShouldBeTrue();

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, destination));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destFile = file.Replace(source, destination);
            File.Copy(file, destFile, overwrite: true);
        }
    }
}
