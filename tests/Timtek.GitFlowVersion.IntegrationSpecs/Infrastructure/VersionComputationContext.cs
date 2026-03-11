using Timtek.GitFlowVersion.Versioning;

namespace Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

/// <summary>Holds the test data and results for an end-to-end version computation scenario.</summary>
internal sealed class VersionComputationContext
{
    /// <summary>Gets the computed version information for the test scenario.</summary>
    public VersionInfo Result { get; }

    public VersionComputationContext(VersionInfo result)
    {
        Result = result;
    }
}
