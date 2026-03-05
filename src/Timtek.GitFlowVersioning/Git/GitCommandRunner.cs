using System.Diagnostics;

namespace Timtek.GitFlowVersioning.Git;

/// <summary>Runs git CLI commands and returns their output.</summary>
public static class GitCommandRunner
{
    /// <summary>
    /// Runs <c>git <paramref name="arguments"/></c> in the given directory and returns stdout.
    /// </summary>
    /// <param name="arguments">Arguments to pass to git.</param>
    /// <param name="workingDirectory">The working directory in which to run git.</param>
    /// <returns>The trimmed stdout of the git command.</returns>
    /// <exception cref="GitCommandException">Thrown if git exits with a non-zero exit code.</exception>
    public static string RunCommand(string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git process.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new GitCommandException(arguments, process.ExitCode, stderr.Trim());

        return stdout.Trim();
    }
}
