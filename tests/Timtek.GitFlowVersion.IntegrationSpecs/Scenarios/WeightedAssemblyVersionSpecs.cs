using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Weighted assembly version is tag-movement independent")]
class when_a_release_branch_gets_more_commits_after_a_build : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("0.2.0")
        .WithBranch("develop")
        .WithCommits(5)
        .WithBranch("release/1.0.1")
        .WithCommits(3)
        .Build();

    It should_have_a_weighted_assembly_sem_ver_reflecting_total_commits = () => Context.Result.AssemblySemVer.ShouldEqual("1.0.1.30009");
}

[Subject("Weighted assembly version is tag-movement independent")]
class when_a_prerelease_tag_is_force_moved_forward_without_renaming : With_end_to_end_version_computation
{
    // Reproduces the reported ClickOnce bug: a "1.0.1-rc.1" tag is force-moved to a later commit
    // instead of being renamed to "-rc.2". Distance-from-tag resets to 0 and the tag's own embedded
    // number stays "1", but real commits have happened, so the weighted AssemblySemVer must still increase.
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("0.2.0")
        .WithBranch("develop")
        .WithCommits(5)
        .WithBranch("release/1.0.1")
        .WithCommits(2)
        .WithTag("1.0.1-rc.1")
        .WithCommits(3)
        .WithTagMovedHere("1.0.1-rc.1")
        .Build();

    It should_still_use_the_tag_verbatim_as_semver = () => Context.Result.SemVer.ShouldEqual("1.0.1-rc.1");
    It should_have_a_higher_weighted_assembly_sem_ver_than_the_original_tagged_build = () => Context.Result.AssemblySemVer.ShouldEqual("1.0.1.30011");
}
