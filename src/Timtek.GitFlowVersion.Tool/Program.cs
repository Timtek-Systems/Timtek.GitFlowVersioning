using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    return Run(args, jsonOptions);
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

static int Run(string[] args, JsonSerializerOptions jsonOptions)
{
    if (IsSnapshotCommand(args))
        return RunSnapshot(args);

    var directory = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
    var commitInfo = GitInfoGatherer.Gather(directory);
    var versionInfo = VersionCalculator.Calculate(commitInfo);
    var json = JsonSerializer.Serialize(versionInfo, jsonOptions);
    Console.WriteLine(json);
    return 0;
}

static bool IsSnapshotCommand(string[] args) =>
    args.Length > 0 && string.Equals(args[0], "snapshot", StringComparison.OrdinalIgnoreCase);

static int RunSnapshot(string[] args)
{
    if (args.Length > 3)
        throw new ArgumentException("Usage: dotnet gitflowversion snapshot [repositoryPath] [outputFile]", nameof(args));

    var repositoryPath = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();
    var outputFile = args.Length > 2 ? args[2] : string.Empty;
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
