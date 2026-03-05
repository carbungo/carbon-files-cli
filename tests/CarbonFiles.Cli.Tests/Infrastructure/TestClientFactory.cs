using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Infrastructure;

/// <summary>
/// Helper for creating test apps with a mock HTTP backend.
/// </summary>
public static class TestClientFactory
{
    public static (CommandAppTester App, MockHttpHandler Handler) CreateApp<T>(bool includeFactory = false) where T : class, ICommand
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var client = new CarbonFilesClient(new CarbonFilesClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            ApiKey = "test-token",
            HttpClient = httpClient,
        });

        var config = new CliConfiguration();
        config.SetProfile("default", "http://localhost", "test-token");
        var factory = new ApiClientFactory(config);

        var services = new ServiceCollection();
        services.AddSingleton(client);
        if (includeFactory)
            services.AddSingleton(factory);

        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<T>("cmd"));

        return (app, handler);
    }
}
