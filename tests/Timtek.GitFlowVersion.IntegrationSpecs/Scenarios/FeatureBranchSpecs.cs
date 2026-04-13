using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Feature branch integration")]
class when_feature_branch_is_ahead_of_tag : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.2.3")
        .WithBranch("feature/new-widget")
        .WithCommits(10)
        .Build();

    It should_have_alpha_label = () => Context.Result.PreReleaseLabel.ShouldEqual("alpha");
    It should_use_tag_distance_as_prerelease_number = () => Context.Result.PreReleaseNumber.ShouldEqual("10");
    It should_produce_correct_semver = () => Context.Result.SemVer.ShouldEqual("1.2.3-alpha.10");
    It should_produce_weighted_assembly_sem_ver = () => Context.Result.AssemblySemVer.ShouldEqual("1.2.3.30010");
    It should_produce_weighted_assembly_file_ver = () => Context.Result.AssemblySemFileVer.ShouldEqual("1.2.3.30010");
}

[Subject("Feature branch integration")]
class when_bugfix_branch_is_ahead_of_tag : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.0.0")
        .WithBranch("bugfix/fix-typo")
        .WithCommits(2)
        .Build();

    It should_have_alpha_label = () => Context.Result.PreReleaseLabel.ShouldEqual("alpha");
    It should_produce_correct_semver = () => Context.Result.SemVer.ShouldEqual("1.0.0-alpha.2");
}

[Subject("Feature branch integration")]
class when_feature_branch_has_escaped_branch_name : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.0.0")
        .WithBranch("feature/my-feature")
        .WithCommits(1)
        .Build();

    It should_escape_slashes = () => Context.Result.EscapedBranchName.ShouldEqual("feature-my-feature");
    It should_preserve_original_branch_name = () => Context.Result.BranchName.ShouldEqual("feature/my-feature");
}
