using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using Timtek.GitFlowVersion.CI;
using Timtek.GitFlowVersion.Scenarios;
using Timtek.GitFlowVersion.Git;
using Timtek.GitFlowVersion.Versioning;

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

try
{
    return Execute(args, jsonOptions);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
catch (GitCommandException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static int Execute(string[] args, JsonSerializerOptions jsonOptions)
{
    var parser = new Parser(configuration => configuration.HelpWriter = Console.Out);
    var parseResult = parser.ParseArguments<CliOptions>(args);

    return parseResult.MapResult(
        options => ExecuteOptions(options, jsonOptions),
        _ => 1);
}

static int ExecuteOptions(CliOptions options, JsonSerializerOptions jsonOptions)
{
    if (!options.Snapshot && string.Equals(options.Path, "snapshot", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("The positional 'snapshot' argument is no longer supported. Use --snapshot.");

    if (options.Snapshot)
        return RunSnapshot(options);

    if (!string.IsNullOrWhiteSpace(options.Output))
        throw new ArgumentException("--output can only be used with --snapshot.");

    if (!string.IsNullOrWhiteSpace(options.RepositoryPath) && !string.IsNullOrWhiteSpace(options.Path))
        throw new ArgumentException("Specify repository path using either [path] or --repository, not both.");

    var directory = !string.IsNullOrWhiteSpace(options.RepositoryPath)
        ? options.RepositoryPath
        : !string.IsNullOrWhiteSpace(options.Path)
            ? options.Path
            : Directory.GetCurrentDirectory();

    var commitInfo = GitInfoGatherer.Gather(directory);
    var versionInfo = VersionCalculator.Calculate(commitInfo);
    var json = JsonSerializer.Serialize(versionInfo, jsonOptions);
    Console.WriteLine(json);
    CiMessageEmitter.Emit(versionInfo, Console.Error.WriteLine);
    return 0;
}

static int RunSnapshot(CliOptions options)
{
    if (!string.IsNullOrWhiteSpace(options.Path))
        throw new ArgumentException("Usage: dotnet gitflowversion --snapshot [--repository <path>] [--output <file>]");

    var repositoryPath = string.IsNullOrWhiteSpace(options.RepositoryPath)
        ? Directory.GetCurrentDirectory()
        : options.RepositoryPath;

    var outputFile = options.Output ?? string.Empty;
    var scenario = VersionScenarioCapture.Capture(repositoryPath);
    var code = ScenarioCodeGenerator.GenerateTestFixture(scenario);

    if (string.IsNullOrWhiteSpace(outputFile))
    {
        Console.WriteLine(code);
        return 0;
    }

    var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputFile));
    if (!string.IsNullOrWhiteSpace(outputDirectory))
        Directory.CreateDirectory(outputDirectory);

    File.WriteAllText(outputFile, code);
    return 0;
}