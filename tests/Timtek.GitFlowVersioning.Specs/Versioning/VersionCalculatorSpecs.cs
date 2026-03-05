using Machine.Specifications;
using Timtek.GitFlowVersioning.Versioning;

namespace Timtek.GitFlowVersioning.Specs.Versioning;

class With_version_calculator_context
{
    protected static GitCommitInfo CommitInfo = null!;
    protected static VersionInfo Result = null!;

    protected static GitCommitInfo BuildCommitInfo(string branchName, string baseTag = "1.2.3", int distance = 0)
        => new GitCommitInfo
        {
            Sha = "abcdef1234567890abcdef1234567890abcdef12",
            BranchName = branchName,
            BaseVersionTag = baseTag,
            CommitDistance = distance,
            HasTag = true
        };
}

[Subject(typeof(VersionCalculator), "main branch at tagged commit")]
class when_computing_version_for_main_branch_at_exact_tag : With_version_calculator_context
{
    Establish context = () => CommitInfo = BuildCommitInfo("main", "1.2.3", distance: 0);
    Because of = () => Result = VersionCalculator.Calculate(CommitInfo);
    It should_have_correct_major = () => Result.Major.ShouldEqual("1");
    It should_have_correct_minor = () => Result.Minor.ShouldEqual("2");
    It should_have_correct_patch = () => Result.Patch.ShouldEqual("3");
    It should_have_empty_prerelease_label = () => Result.PreReleaseLabel.ShouldBeEmpty();
    It should_have_semver_without_prerelease = () => Result.SemVer.ShouldEqual("1.2.3");
    It should_have_assembly_sem_ver = () => Result.AssemblySemVer.ShouldEqual("1.2.0.0");
    It should_have_assembly_file_ver = () => Result.AssemblySemFileVer.ShouldEqual("1.2.3.0");
}

[Subject(typeof(VersionCalculator), "main branch ahead of tag")]
class when_computing_version_for_main_branch_5_commits_ahead_of_tag : With_version_calculator_context
{
    Establish context = () => CommitInfo = BuildCommitInfo("main", "1.2.3", distance: 5);
    Because of = () => Result = VersionCalculator.Calculate(CommitInfo);
    It should_have_patch_incremented_by_distance = () => Result.Patch.ShouldEqual("8");
    It should_have_semver_1_2_8 = () => Result.SemVer.ShouldEqual("1.2.8");
    It should_have_empty_prerelease_label = () => Result.PreReleaseLabel.ShouldBeEmpty();
}

[Subject(typeof(VersionCalculator), "develop branch")]
class when_computing_version_for_develop_branch : With_version_calculator_context
{
    Establish context = () => CommitInfo = BuildCommitInfo("develop", "1.2.3", distance: 7);
    Because of = () => Result = VersionCalculator.Calculate(CommitInfo);
    It should_have_minor_bumped = () => Result.Minor.ShouldEqual("3");
    It should_have_patch_zero = () => Result.Patch.ShouldEqual("0");
    It should_have_alpha_prerelease_label = () => Result.PreReleaseLabel.ShouldEqual("alpha");
    It should_have_prerelease_number_equal_to_distance = () => Result.PreReleaseNumber.ShouldEqual("7");
    It should_have_correct_semver = () => Result.SemVer.ShouldEqual("1.3.0-alpha.7");
}

[Subject(typeof(VersionCalculator), "release branch")]
class when_computing_version_for_release_branch : With_version_calculator_context
{
    Establish context = () => CommitInfo = BuildCommitInfo("release/1.3.0", "1.2.3", distance: 3);
    Because of = () => Result = VersionCalculator.Calculate(CommitInfo);
    It should_have_beta_prerelease_label = () => Result.PreReleaseLabel.ShouldEqual("beta");
    It should_have_prerelease_number_equal_to_distance = () => Result.PreReleaseNumber.ShouldEqual("3");
    It should_have_correct_semver = () => Result.SemVer.ShouldEqual("1.2.3-beta.3");
}

[Subject(typeof(VersionCalculator), "hotfix branch")]
class when_computing_version_for_hotfix_branch : With_version_calculator_context
{
    Establish context = () => CommitInfo = BuildCommitInfo("hotfix/fix-critical", "1.2.3", distance: 2);
    Because of = () => Result = VersionCalculator.Calculate(CommitInfo);
    It should_have_beta_prerelease_label = () => Result.PreReleaseLabel.ShouldEqual("beta");
    It should_have_prerelease_number_2 = () => Result.PreReleaseNumber.ShouldEqual("2");
    It should_have_correct_semver = () => Result.SemVer.ShouldEqual("1.2.3-beta.2");
}

[Subject(typeof(VersionCalculator), "feature branch")]
class when_computing_version_for_feature_branch : With_version_calculator_context
{
    Establish context = () => CommitInfo = BuildCommitInfo("feature/my-feature", "1.2.3", distance: 10);
    Because of = () => Result = VersionCalculator.Calculate(CommitInfo);
    It should_have_alpha_prerelease_label = () => Result.PreReleaseLabel.ShouldEqual("alpha");
    It should_have_prerelease_number_10 = () => Result.PreReleaseNumber.ShouldEqual("10");
    It should_have_correct_semver = () => Result.SemVer.ShouldEqual("1.2.3-alpha.10");
}

[Subject(typeof(VersionCalculator), "escaped branch name")]
class when_computing_version_for_feature_branch_with_slash : With_version_calculator_context
{
    Establish context = () => CommitInfo = BuildCommitInfo("feature/foo", "1.0.0", distance: 1);
    Because of = () => Result = VersionCalculator.Calculate(CommitInfo);
    It should_have_escaped_branch_name = () => Result.EscapedBranchName.ShouldEqual("feature-foo");
    It should_have_correct_branch_name = () => Result.BranchName.ShouldEqual("feature/foo");
}

[Subject(typeof(VersionCalculator), "short SHA")]
class when_computing_version_sha_fields_are_populated : With_version_calculator_context
{
    Establish context = () => CommitInfo = BuildCommitInfo("main", "1.0.0", distance: 0);
    Because of = () => Result = VersionCalculator.Calculate(CommitInfo);
    It should_have_full_sha = () => Result.Sha.ShouldEqual("abcdef1234567890abcdef1234567890abcdef12");
    It should_have_short_sha_of_7_chars = () => Result.ShortSha.ShouldEqual("abcdef1");
}

[Subject(typeof(VersionCalculator), "fallback version when no tag")]
class when_computing_version_with_fallback_base_version : With_version_calculator_context
{
    Establish context = () => CommitInfo = new GitCommitInfo
    {
        Sha = "abcdef1234567890abcdef1234567890abcdef12",
        BranchName = "feature/test",
        BaseVersionTag = "0.1.0",
        CommitDistance = 5,
        HasTag = false
    };
    Because of = () => Result = VersionCalculator.Calculate(CommitInfo);
    It should_use_fallback_base_version = () => Result.MajorMinorPatch.ShouldEqual("0.1.0");
    It should_have_alpha_label = () => Result.PreReleaseLabel.ShouldEqual("alpha");
    It should_have_correct_semver = () => Result.SemVer.ShouldEqual("0.1.0-alpha.5");
}
