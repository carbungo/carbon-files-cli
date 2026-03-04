using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarbonFiles.Cli.Infrastructure;

public sealed class CliConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string _filePath = null!;

    public string ActiveProfile { get; set; } = "default";
    public Dictionary<string, Profile> Profiles { get; set; } = new();

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cf", "config.json");

    public static CliConfiguration Load(string? path = null)
    {
        path ??= DefaultPath;
        CliConfiguration config;

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            config = JsonSerializer.Deserialize<CliConfiguration>(json, JsonOptions) ?? new();
        }
        else
        {
            config = new CliConfiguration();
        }

        config._filePath = path;
        return config;
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public void SetProfile(string name, string url, string token)
    {
        Profiles[name] = new Profile { Url = url.TrimEnd('/'), Token = token };
    }

    public Profile? GetActiveProfile()
        => Profiles.GetValueOrDefault(ActiveProfile);
}

public sealed class Profile
{
    public string Url { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    [JsonIgnore]
    public string MaskedToken
    {
        get
        {
            if (string.IsNullOrEmpty(Token)) return string.Empty;
            // cf4_prefix_secret -> cf4_prefix_****
            var underscoreCount = 0;
            for (var i = 0; i < Token.Length; i++)
            {
                if (Token[i] == '_') underscoreCount++;
                if (underscoreCount == 2)
                    return string.Concat(Token.AsSpan(0, i + 1), "****");
            }
            return "****";
        }
    }
}
