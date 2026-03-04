using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarbonFiles.Client;
using Refit;
using Spectre.Console;

namespace CarbonFiles.Cli.Infrastructure;

public sealed class ApiClientFactory(CliConfiguration config)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly RefitSettings RefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(JsonOptions)
    };

    public IAnsiConsole? VerboseConsole { get; set; }

    public ICarbonFilesApi Create(string? profileName = null)
    {
        var profile = GetProfile(profileName);
        var httpClient = BuildHttpClient(profile);
        return RestService.For<ICarbonFilesApi>(httpClient, RefitSettings);
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

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", profile.Token);

        return httpClient;
    }
}
