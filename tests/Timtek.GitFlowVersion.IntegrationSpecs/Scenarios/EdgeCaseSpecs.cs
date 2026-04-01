using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Scenarios;

[Subject("Edge case integration")]
class when_head_is_at_exact_tag_with_zero_distance : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("2.0.0")
        .WithCommits(3)
        .WithTag("2.0.3")
        .Build();

    It should_produce_stable_release = () => Context.Result.PreReleaseLabel.ShouldBeEmpty();
    It should_use_latest_tag = () => Context.Result.SemVer.ShouldEqual("2.0.3");
}

[Subject("Edge case integration")]
class when_repository_has_multiple_tags_and_head_is_between_them : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.0.0")
        .WithCommits(3)
        .WithTag("1.1.0")
        .WithCommits(2)
        .Build();

    It should_use_nearest_tag_as_base = () => Context.Result.SemVer.ShouldEqual("1.1.2");
}

[Subject("Edge case integration")]
class when_tag_uses_uppercase_v_prefix : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("V3.0.0")
        .WithCommits(1)
        .Build();

    It should_strip_uppercase_v_prefix = () => Context.Result.Major.ShouldEqual("3");
    It should_produce_correct_semver = () => Context.Result.SemVer.ShouldEqual("3.0.1");
}

[Subject("Edge case integration")]
class when_ci_build_tags_coexist_with_release_tag_at_head : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("3.0.5")
        .WithTag("build-3.0.5")
        .Build();

    It should_ignore_ci_build_tag = () => Context.Result.PreReleaseLabel.ShouldBeEmpty();
    It should_use_semver_release_tag = () => Context.Result.SemVer.ShouldEqual("3.0.5");
}

[Subject("Edge case integration")]
class when_only_ci_build_tags_exist_with_no_semver_release_tag : With_end_to_end_version_computation
{
    // Fallback uses rev-list --count HEAD (3 commits) as distance from 0.1.0 baseline.
    // On main, distance is added to patch: 0.1.0 + 3 = 0.1.3, no prerelease label.
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("build-0.1.0")
        .WithCommits(2)
        .Build();

    It should_not_use_ci_build_tag_as_semver_base = () => Context.Result.Major.ShouldEqual("0");
    It should_produce_version_from_commit_count_fallback = () => Context.Result.SemVer.ShouldEqual("0.1.3");
    It should_have_no_prerelease_label_on_main = () => Context.Result.PreReleaseLabel.ShouldBeEmpty();
}

[Subject("Edge case integration")]
class when_a_non_semver_tag_is_nearer_than_a_valid_release_tag : With_end_to_end_version_computation
{
    Establish context = () => Context = Builder
        .WithInitialCommit()
        .WithTag("1.0.0")
        .WithCommits(2)
        .WithTag("v1.0.2-rc.1")
        .Build();

    It should_ignore_the_nearer_non_semver_tag = () => Context.Result.PreReleaseLabel.ShouldBeEmpty();
    It should_fall_back_to_the_nearest_valid_release_tag = () => Context.Result.SemVer.ShouldEqual("1.0.2");
}
