using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Timtek.GitFlowVersion.Git;
using Timtek.GitFlowVersion.Versioning;

var directory = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

try
{
    var commitInfo = GitInfoGatherer.Gather(directory);
    var versionInfo = VersionCalculator.Calculate(commitInfo);

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    var json = JsonSerializer.Serialize(versionInfo, options);
    Console.WriteLine(json);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

return 0;
