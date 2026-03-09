namespace Timtek.GitFlowVersioning.TestSubject;

/// <summary>
///     A placeholder class used as a test subject for verifying that
///     GitFlow versioning correctly stamps NuGet package metadata.
/// </summary>
public static class Placeholder
{
    /// <summary>Gets the assembly version as reported by the runtime.</summary>
    public static string AssemblyVersion =>
        typeof(Placeholder).Assembly.GetName().Version?.ToString() ?? "unknown";
}
