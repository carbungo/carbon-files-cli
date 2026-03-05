using CarbonFiles.Client;
using Spectre.Console;

namespace CarbonFiles.Cli.Infrastructure;

public sealed class ApiClientFactory(CliConfiguration config)
{
    public IAnsiConsole? VerboseConsole { get; set; }

    public CarbonFilesClient Create(string? profileName = null)
    {
        var profile = GetProfile(profileName);

        var options = new CarbonFilesClientOptions
        {
            BaseAddress = new Uri(profile.Url),
            ApiKey = profile.Token,
            HttpClient = BuildHttpClient(profile),
        };

        return new CarbonFilesClient(options);
    }

    public Profile GetProfile(string? profileName = null)
    {
        var profile = profileName is not null
            ? config.Profiles.GetValueOrDefault(profileName)
            : config.GetActiveProfile();

        if (profile is null)
        {
            throw new InvalidOperationException(
                "No server configured. Run: cf config set --url <url> --token <token>");
        }

        return profile;
    }

    public HttpClient CreateHttpClient(string? profileName = null)
    {
        var profile = GetProfile(profileName);
        return BuildHttpClient(profile);
    }

    private HttpClient BuildHttpClient(Profile profile)
    {
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
