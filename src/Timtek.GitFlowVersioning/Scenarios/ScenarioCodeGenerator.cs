using System.Text;

namespace Timtek.GitFlowVersion.Scenarios;

/// <summary>Generates C# test fixture code from a captured version scenario.</summary>
public static class ScenarioCodeGenerator
{
    /// <summary>Generates a complete MSpec test fixture from a captured scenario.</summary>
    /// <param name="scenario">The captured scenario containing builder steps and expected version.</param>
    /// <returns>A C# code fragment for the Establish and It clauses of an integration test.</returns>
    public static string GenerateTestFixture(CapturedVersionScenario scenario)
    {
        if (scenario is null)
            throw new ArgumentNullException(nameof(scenario));

        var sb = new StringBuilder();
        var expected = scenario.ExpectedVersion;

        sb.AppendLine($"// Captured scenario: {scenario.Scenario}");
        sb.AppendLine($"// Branch: {expected.BranchName}");
        sb.AppendLine($"// Expected SemVer: {expected.SemVer}");
        sb.AppendLine();
        sb.Append("Establish context = () => Context = Builder");

        foreach (var step in scenario.Steps)
        {
            sb.AppendLine();
            sb.Append($"    {step.ToCode()}");
        }

        sb.AppendLine();
        sb.AppendLine("    .Build();");
        sb.AppendLine();
        sb.Append($"It should_produce_expected_semver = () => Context.Result.SemVer.ShouldEqual(\"{expected.SemVer}\");");

        return sb.ToString();
    }
}
