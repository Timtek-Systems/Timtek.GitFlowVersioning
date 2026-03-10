using Machine.Specifications;
using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.Specs.Versioning;

[Subject(typeof(BranchClassifier))]
class when_classifying_main_branch
{
    static BranchType result;
    Because of = () => result = BranchClassifier.Classify("main");
    It should_be_main = () => result.ShouldEqual(BranchType.Main);
}

[Subject(typeof(BranchClassifier))]
class when_classifying_master_branch
{
    static BranchType result;
    Because of = () => result = BranchClassifier.Classify("master");
    It should_be_main = () => result.ShouldEqual(BranchType.Main);
}

[Subject(typeof(BranchClassifier))]
class when_classifying_develop_branch
{
    static BranchType result;
    Because of = () => result = BranchClassifier.Classify("develop");
    It should_be_develop = () => result.ShouldEqual(BranchType.Develop);
}

[Subject(typeof(BranchClassifier))]
class when_classifying_release_branch
{
    static BranchType result;
    Because of = () => result = BranchClassifier.Classify("release/1.2.0");
    It should_be_release = () => result.ShouldEqual(BranchType.Release);
}

[Subject(typeof(BranchClassifier))]
class when_classifying_hotfix_branch
{
    static BranchType result;
    Because of = () => result = BranchClassifier.Classify("hotfix/fix-bug");
    It should_be_hotfix = () => result.ShouldEqual(BranchType.Hotfix);
}

[Subject(typeof(BranchClassifier))]
class when_classifying_feature_branch
{
    static BranchType result;
    Because of = () => result = BranchClassifier.Classify("feature/my-feature");
    It should_be_other = () => result.ShouldEqual(BranchType.Other);
}

[Subject(typeof(BranchClassifier))]
class when_classifying_empty_branch_name
{
    static BranchType result;
    Because of = () => result = BranchClassifier.Classify(string.Empty);
    It should_be_other = () => result.ShouldEqual(BranchType.Other);
}

[Subject(typeof(BranchClassifier))]
class when_classifying_main_branch_with_uppercase
{
    static BranchType result;
    Because of = () => result = BranchClassifier.Classify("MAIN");
    It should_be_main = () => result.ShouldEqual(BranchType.Main);
}
