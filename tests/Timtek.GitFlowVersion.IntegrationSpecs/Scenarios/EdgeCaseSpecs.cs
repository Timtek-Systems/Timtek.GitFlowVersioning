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
