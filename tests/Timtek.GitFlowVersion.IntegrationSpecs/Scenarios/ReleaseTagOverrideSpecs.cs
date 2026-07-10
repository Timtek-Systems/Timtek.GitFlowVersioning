using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Release branch tag override integration")]
class when_release_branch_is_tagged_with_a_matching_prerelease_tag : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("0.2.0")
        .WithBranch("develop")
        .WithCommits(5)
        .WithBranch("release/1.0.1")
        .WithCommits(3)
        .WithTag("1.0.1-rc.1")
        .Build();

    It should_use_the_tag_verbatim_as_semver = () => Context.Result.SemVer.ShouldEqual("1.0.1-rc.1");
    It should_have_a_build_metadata_of_zero = () => Context.Result.FullSemVer.ShouldEqual("1.0.1-rc.1+0");
    It should_have_the_tag_prerelease_label = () => Context.Result.PreReleaseLabel.ShouldEqual("rc");
}

[Subject("Release branch tag override integration")]
class when_a_commit_is_added_after_the_release_tag_override : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("0.2.0")
        .WithBranch("develop")
        .WithCommits(5)
        .WithBranch("release/1.0.1")
        .WithCommits(3)
        .WithTag("1.0.1-rc.1")
        .WithCommits(1)
        .Build();

    It should_revert_to_normal_beta_logic = () => Context.Result.SemVer.ShouldEqual("1.0.1-beta.4");
}

[Subject("Release branch tag override integration")]
class when_release_branch_is_tagged_with_a_mismatched_prerelease_tag : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("0.2.0")
        .WithBranch("develop")
        .WithCommits(5)
        .WithBranch("release/1.0.1")
        .WithCommits(3)
        .WithTag("2.0.0-rc.1")
        .Build();

    It should_ignore_the_mismatched_tag = () => Context.Result.SemVer.ShouldEqual("1.0.1-beta.3");
}
