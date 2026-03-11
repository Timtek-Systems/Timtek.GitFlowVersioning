using Machine.Specifications;
using Timtek.GitFlowVersion.IntegrationSpecs.Infrastructure;

namespace Timtek.GitFlowVersion.IntegrationSpecs;

/// <summary>
/// Base context for end-to-end integration specs that build a temporary Git
/// repository, compute version variables, and expose a Context-Builder pattern.
/// </summary>
class With_end_to_end_version_computation
{
    protected static VersionComputationContext Context = null!;
    protected static VersionComputationBuilder Builder = null!;

    Establish context = () => Builder = new VersionComputationBuilder();

    Cleanup after = () => Builder?.Dispose();
}
