using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;
using Timtek.GitFlowVersion.Scenarios;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Snapshot capture integration")]
class when_capturing_a_feature_branch_repository_as_a_snapshot_fixture : With_end_to_end_version_computation
{
    static CapturedVersionScenario Scenario = null!;

    Establish context = () =>
    {
        Context = Builder
            .WithInitialCommit()
            .WithTag("1.2.3")
            .WithBranch("feature/snapshot-fixture")
            .WithCommits(2, "Feature work")
            .Build();

        Scenario = VersionScenarioCapture.Capture(Builder.RepoPath);
    };

    It should_assign_a_stable_default_scenario_name = () => Scenario.Scenario.ShouldEqual("feature-snapshot-fixture-1.2.3-alpha.2");
    It should_capture_the_expected_semver = () => Scenario.ExpectedVersion.SemVer.ShouldEqual("1.2.3-alpha.2");
    It should_extract_initial_commit_step = () => Scenario.Steps[0].ShouldEqual(BuilderStep.InitialCommit());
    It should_extract_tag_step = () => Scenario.Steps[1].ShouldEqual(BuilderStep.Tag("1.2.3"));
    It should_extract_branch_step = () => Scenario.Steps[2].ShouldEqual(BuilderStep.Branch("feature/snapshot-fixture"));
    It should_extract_commits_step = () => Scenario.Steps[3].ShouldEqual(BuilderStep.Commits(2));
}
