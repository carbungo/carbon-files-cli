using CarbonFiles.Client;
using Spectre.Console;

namespace CarbonFiles.Cli.Infrastructure;

public sealed class ApiClientFactory(CliConfiguration config, Func<Profile, HttpClient>? httpClientBuilder = null,
    Func<string, string?>? environment = null)
{
    public IAnsiConsole? VerboseConsole { get; set; }

    public CarbonFilesClient Create(string? profileName = null, string? tokenOverride = null)
    {
        var profile = GetProfile(profileName, tokenOverride);
        var token = tokenOverride ?? profile.Token;

        var options = new CarbonFilesClientOptions
        {
            BaseAddress = new Uri(profile.Url),
            ApiKey = IsUploadToken(token) ? null : token,
            HttpClient = BuildHttpClient(profile),
        };

        return new CarbonFilesClient(options);
    }

    public Profile GetProfile(string? profileName = null, string? tokenOverride = null)
    {
        var saved = profileName is not null
            ? config.Profiles.GetValueOrDefault(profileName)
            : config.GetActiveProfile();
        var url = (environment ?? Environment.GetEnvironmentVariable)("CF_URL");
        var token = (environment ?? Environment.GetEnvironmentVariable)("CF_TOKEN");
        if (profileName is null && (!string.IsNullOrWhiteSpace(url) || !string.IsNullOrWhiteSpace(token) || tokenOverride is not null))
        {
            var effective = new Profile
            {
                Url = !string.IsNullOrWhiteSpace(url) ? url.TrimEnd('/') : saved?.Url ?? string.Empty,
                Token = tokenOverride ?? (!string.IsNullOrWhiteSpace(token) ? token : saved?.Token ?? string.Empty),
                FrontendUrl = saved?.FrontendUrl,
            };
            if (!string.IsNullOrWhiteSpace(effective.Url) && !string.IsNullOrWhiteSpace(effective.Token))
                return effective;
        }

        if (saved is null)
        {
            throw new InvalidOperationException(
                "No server configured. Run: cf config set --url <url> --token <token>");
        }

        return saved;
    }

    public static bool IsUploadToken(string? token) => token?.StartsWith("cfu_", StringComparison.Ordinal) == true;

    public HttpClient CreateHttpClient(string? profileName = null, string? tokenOverride = null)
    {
        var profile = GetProfile(profileName, tokenOverride);
        return BuildHttpClient(profile);
    }

    private HttpClient BuildHttpClient(Profile profile)
    {
        if (httpClientBuilder is not null)
        {
            var provided = httpClientBuilder(profile);
            provided.Timeout = Timeout.InfiniteTimeSpan;
            return provided;
        }
        HttpClient httpClient;

        if (VerboseConsole is not null)
        {
            var handler = new VerboseLoggingHandler(VerboseConsole);
            httpClient = new HttpClient(handler) { BaseAddress = new Uri(profile.Url) };
        }
        else
        {
            httpClient = new HttpClient { BaseAddress = new Uri(profile.Url) };
        }

        // Disable the default 100-second timeout so large file uploads don't fail mid-stream.
        // Cancellation is handled via the CancellationToken passed to each operation (e.g. Ctrl+C).
        httpClient.Timeout = Timeout.InfiniteTimeSpan;

        return httpClient;
    }
}
