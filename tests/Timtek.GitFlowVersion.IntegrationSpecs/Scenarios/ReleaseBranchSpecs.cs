using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Release branch integration")]
class when_release_branch_has_version_suffix_and_commits_since_develop : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("0.2.0")
        .WithBranch("develop")
        .WithCommits(5)
        .WithBranch("release/1.0.1")
        .WithCommits(3)
        .Build();

    It should_use_branch_name_as_base_version = () => Context.Result.MajorMinorPatch.ShouldEqual("1.0.1");
    It should_have_beta_label = () => Context.Result.PreReleaseLabel.ShouldEqual("beta");
    It should_count_commits_since_branch_point = () => Context.Result.PreReleaseNumber.ShouldEqual("3");
    It should_produce_correct_semver = () => Context.Result.SemVer.ShouldEqual("1.0.1-beta.3");
}

[Subject("Release branch integration")]
class when_release_branch_has_single_commit_after_branching : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("0.2.0")
        .WithBranch("develop")
        .WithCommits(2)
        .WithBranch("release/1.0.1")
        .WithCommits(1)
        .Build();

    It should_produce_beta_1 = () => Context.Result.SemVer.ShouldEqual("1.0.1-beta.1");
}

[Subject("Release branch integration")]
class when_release_branch_has_non_version_suffix : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.2.3")
        .WithBranch("develop")
        .WithCommits(2)
        .WithBranch("release/candidate")
        .WithCommits(2)
        .Build();

    It should_fall_back_to_tag_base_version = () => Context.Result.MajorMinorPatch.ShouldEqual("1.2.3");
    It should_have_beta_label = () => Context.Result.PreReleaseLabel.ShouldEqual("beta");
    It should_produce_correct_semver = () => Context.Result.SemVer.ShouldEqual("1.2.3-beta.2");
}

[Subject("Release branch integration")]
class when_release_branch_is_at_branch_point_with_no_additional_commits : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.0.0")
        .WithBranch("develop")
        .WithCommits(3)
        .WithBranch("release/1.1.0")
        .Build();

    It should_produce_beta_0 = () => Context.Result.SemVer.ShouldEqual("1.1.0-beta.0");
    It should_have_zero_prerelease_number = () => Context.Result.PreReleaseNumber.ShouldEqual("0");
}
