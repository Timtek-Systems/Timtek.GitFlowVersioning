using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Hotfix branch integration")]
class when_hotfix_branch_has_version_suffix_and_commits_since_main : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.0.0")
        .WithCommits(2)
        .WithBranch("hotfix/1.0.3")
        .WithCommits(1)
        .Build();

    It should_use_branch_name_as_base_version = () => Context.Result.MajorMinorPatch.ShouldEqual("1.0.3");
    It should_have_beta_label = () => Context.Result.PreReleaseLabel.ShouldEqual("beta");
    It should_count_commits_since_main = () => Context.Result.PreReleaseNumber.ShouldEqual("1");
    It should_produce_correct_semver = () => Context.Result.SemVer.ShouldEqual("1.0.3-beta.1");
}

[Subject("Hotfix branch integration")]
class when_hotfix_branch_has_non_version_suffix : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.2.3")
        .WithBranch("hotfix/fix-critical")
        .WithCommits(2)
        .Build();

    It should_fall_back_to_tag_base_version = () => Context.Result.MajorMinorPatch.ShouldEqual("1.2.3");
    It should_have_beta_label = () => Context.Result.PreReleaseLabel.ShouldEqual("beta");
    It should_produce_correct_semver = () => Context.Result.SemVer.ShouldEqual("1.2.3-beta.2");
}

[Subject("Hotfix branch integration")]
class when_hotfix_branch_has_multiple_commits : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("2.0.0")
        .WithBranch("hotfix/2.0.1")
        .WithCommits(4)
        .Build();

    It should_produce_correct_semver = () => Context.Result.SemVer.ShouldEqual("2.0.1-beta.4");
    It should_count_all_commits_on_hotfix = () => Context.Result.PreReleaseNumber.ShouldEqual("4");
}
