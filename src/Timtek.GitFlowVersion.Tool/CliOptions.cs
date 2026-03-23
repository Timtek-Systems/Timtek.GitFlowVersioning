using CommandLine;

sealed class CliOptions
{
    [Option("snapshot", Required = false, HelpText = "Capture a repository topology as generated C# fixture code.")]
    public bool Snapshot { get; set; }

    [Option('r', "repository", Required = false, HelpText = "Repository path. Defaults to current directory.")]
    public string? RepositoryPath { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output file path for snapshot fixture code.")]
    public string? Output { get; set; }

    [Value(0, MetaName = "path", HelpText = "Repository path for standard version computation.")]
    public string? Path { get; set; }
}