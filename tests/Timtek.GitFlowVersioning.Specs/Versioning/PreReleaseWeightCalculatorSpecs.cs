using Machine.Specifications;
using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.Specs.Versioning;

[Subject(typeof(PreReleaseWeightCalculator), "main branch weights")]
class when_calculating_pre_release_weight_for_main_branch
{
    static int result;

    Because of = () => result = PreReleaseWeightCalculator.GetWeight("main");

    It should_use_gitversion_main_weight = () => result.ShouldEqual(55000);
}

[Subject(typeof(PreReleaseWeightCalculator), "master branch weights")]
class when_calculating_pre_release_weight_for_master_branch
{
    static int result;

    Because of = () => result = PreReleaseWeightCalculator.GetWeight("master");

    It should_use_gitversion_main_weight = () => result.ShouldEqual(55000);
}

[Subject(typeof(PreReleaseWeightCalculator), "develop branch weights")]
class when_calculating_pre_release_weight_for_develop_branch
{
    static int result;

    Because of = () => result = PreReleaseWeightCalculator.GetWeight("develop");

    It should_use_gitversion_develop_weight = () => result.ShouldEqual(0);
}

[Subject(typeof(PreReleaseWeightCalculator), "release branch weights")]
class when_calculating_pre_release_weight_for_release_branch
{
    static int result;

    Because of = () => result = PreReleaseWeightCalculator.GetWeight("release/1.2.3");

    It should_use_gitversion_release_weight = () => result.ShouldEqual(30000);
}

[Subject(typeof(PreReleaseWeightCalculator), "hotfix branch weights")]
class when_calculating_pre_release_weight_for_hotfix_branch
{
    static int result;

    Because of = () => result = PreReleaseWeightCalculator.GetWeight("hotfix/1.2.4");

    It should_use_gitversion_hotfix_weight = () => result.ShouldEqual(30000);
}

[Subject(typeof(PreReleaseWeightCalculator), "feature branch weights")]
class when_calculating_pre_release_weight_for_feature_branch
{
    static int result;

    Because of = () => result = PreReleaseWeightCalculator.GetWeight("feature/my-feature");

    It should_use_gitversion_feature_weight = () => result.ShouldEqual(30000);
}
