using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Captured scenario integration")]
class when_replaying_the_captured_timtek_patterns_release_1_0_1_scenario : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithCommits(18)
        .WithTag("v0.2.0")
        .WithCommits(1)
        .WithBranch("develop")
        .WithBranch("release/1.0.1")
        .WithCommits(9)
        .Build();

    It should_produce_expected_semver = () => Context.Result.SemVer.ShouldEqual("1.0.1-beta.9");
    It should_preserve_the_expected_branch_name = () => Context.Result.BranchName.ShouldEqual("release/1.0.1");
    It should_have_beta_label = () => Context.Result.PreReleaseLabel.ShouldEqual("beta");
    It should_have_correct_prerelease_number = () => Context.Result.PreReleaseNumber.ShouldEqual("9");
}
