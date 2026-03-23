using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Main branch integration")]
class when_main_branch_is_at_exact_tagged_commit : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.0.0")
        .Build();

    It should_produce_stable_semver = () => Context.Result.SemVer.ShouldEqual("1.0.0");
    It should_have_empty_prerelease_label = () => Context.Result.PreReleaseLabel.ShouldBeEmpty();
    It should_have_correct_major = () => Context.Result.Major.ShouldEqual("1");
    It should_have_correct_minor = () => Context.Result.Minor.ShouldEqual("0");
    It should_have_correct_patch = () => Context.Result.Patch.ShouldEqual("0");
}

[Subject("Main branch integration")]
class when_main_branch_is_3_commits_ahead_of_tag : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.2.0")
        .WithCommits(3)
        .Build();

    It should_add_distance_to_patch = () => Context.Result.Patch.ShouldEqual("3");
    It should_produce_stable_semver = () => Context.Result.SemVer.ShouldEqual("1.2.3");
    It should_have_empty_prerelease_label = () => Context.Result.PreReleaseLabel.ShouldBeEmpty();
}

[Subject("Main branch integration")]
class when_main_branch_has_no_tags : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithCommits(4)
        .Build();

    It should_use_fallback_base_version = () => Context.Result.Major.ShouldEqual("0");
    It should_have_empty_prerelease_label = () => Context.Result.PreReleaseLabel.ShouldBeEmpty();
}

[Subject("Main branch integration")]
class when_main_branch_has_v_prefixed_tag : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("v2.1.0")
        .WithCommits(1)
        .Build();

    It should_strip_v_prefix_and_compute_version = () => Context.Result.SemVer.ShouldEqual("2.1.1");
    It should_have_correct_major = () => Context.Result.Major.ShouldEqual("2");
}
