using System.Collections.Generic;
using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.Scenarios;

/// <summary>
/// Represents a captured repository scenario containing the builder steps
/// needed to reconstruct the repository and the expected version output.
/// </summary>
public sealed class CapturedVersionScenario
{
    /// <summary>Gets or sets a stable scenario name for the captured repository state.</summary>
    public string Scenario { get; set; } = string.Empty;

    /// <summary>Gets or sets the builder steps that reconstruct the repository topology.</summary>
    public IReadOnlyList<BuilderStep> Steps { get; set; } = new List<BuilderStep>();

    /// <summary>Gets or sets the expected version outputs produced from the captured topology.</summary>
    public VersionInfo ExpectedVersion { get; set; } = new VersionInfo();
}
