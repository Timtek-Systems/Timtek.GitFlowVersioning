namespace Timtek.GitFlowVersioning.Git;

/// <summary>Thrown when a git CLI command exits with a non-zero exit code.</summary>
public class GitCommandException : Exception
{
    /// <summary>Initializes a new instance of <see cref="GitCommandException"/>.</summary>
    public GitCommandException(string arguments, int exitCode, string stderr)
        : base($"git {arguments} exited with code {exitCode}: {stderr}")
    {
        Arguments = arguments;
        ExitCode = exitCode;
        Stderr = stderr;
    }

    /// <summary>Gets the git arguments that were passed.</summary>
    public string Arguments { get; }

    /// <summary>Gets the process exit code.</summary>
    public int ExitCode { get; }

    /// <summary>Gets the standard error output from git.</summary>
    public string Stderr { get; }
}
