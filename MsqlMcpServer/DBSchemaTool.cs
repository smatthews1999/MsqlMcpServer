using System.Runtime.CompilerServices;
using System.Text;

namespace MsqlMcpServer;

/// <summary>
/// Internal utility for retrieving database schema from entity model files.
/// Used by NaturalLanguageQueryTool to provide schema context to Claude.
/// </summary>
public static class DBSchemaTool
{
    public static string GetSchema([CallerFilePath] string sourceFilePath = "")
    {
        // Get Models folder path relative to this source file
        var sourceDir = Path.GetDirectoryName(sourceFilePath);
        var modelsPath = Path.Combine(sourceDir!, "Models");

        if (!Directory.Exists(modelsPath))
            return $"Error: Models directory not found at {modelsPath}";

        var sb = new StringBuilder();
        var files = Directory.GetFiles(modelsPath, "*.cs")
            .Where(f => !f.EndsWith("Context.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => Path.GetFileName(f));

        foreach (var file in files)
        {
            sb.AppendLine($"// === {Path.GetFileName(file)} ===");
            sb.AppendLine(File.ReadAllText(file));
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
