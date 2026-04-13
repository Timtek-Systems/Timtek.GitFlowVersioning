using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Develop branch integration")]
class when_develop_branch_is_ahead_of_tag_on_main : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.2.0")
        .WithBranch("develop")
        .WithCommits(7)
        .Build();

    It should_bump_minor = () => Context.Result.Minor.ShouldEqual("3");
    It should_reset_patch_to_zero = () => Context.Result.Patch.ShouldEqual("0");
    It should_have_alpha_label = () => Context.Result.PreReleaseLabel.ShouldEqual("alpha");
    It should_use_distance_as_prerelease_number = () => Context.Result.PreReleaseNumber.ShouldEqual("7");
    It should_produce_correct_semver = () => Context.Result.SemVer.ShouldEqual("1.3.0-alpha.7");
    It should_produce_weighted_assembly_sem_ver = () => Context.Result.AssemblySemVer.ShouldEqual("1.3.0.7");
    It should_produce_weighted_assembly_file_ver = () => Context.Result.AssemblySemFileVer.ShouldEqual("1.3.0.7");
}

[Subject("Develop branch integration")]
class when_develop_branch_has_no_tags : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithBranch("develop")
        .WithCommits(3)
        .Build();

    It should_use_fallback_base_with_minor_bump = () => Context.Result.Minor.ShouldEqual("2");
    It should_have_alpha_label = () => Context.Result.PreReleaseLabel.ShouldEqual("alpha");
}
