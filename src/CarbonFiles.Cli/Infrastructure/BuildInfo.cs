using System.Reflection;

namespace CarbonFiles.Cli.Infrastructure;

public static class BuildInfo
{
    private static readonly Assembly Assembly = typeof(BuildInfo).Assembly;

    public static string Version => Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static string InformationalVersion =>
        Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? Version;

    public static string GitCommit => GetMetadata("GitCommit") ?? "unknown";

    public static string GitCommitFull => GetMetadata("GitCommitFull") ?? "unknown";

    public static string BuildDate => GetMetadata("BuildDate") ?? "unknown";

    private static string? GetMetadata(string key) =>
        Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value;
}
