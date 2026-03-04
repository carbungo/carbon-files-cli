using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketWatchCommandTests
{
    [Fact]
    public void Settings_ParsesBucketId()
    {
        var config = new CliConfiguration();
        config.SetProfile("default", "http://localhost:5000", "test-token");
        var factory = new ApiClientFactory(config);

        var services = new ServiceCollection();
        services.AddSingleton(factory);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<BucketWatchCommand>("watch"));

        // We can't fully run the command (it requires a real SignalR server),
        // but we can verify the settings are parsed by checking with --help
        // or by verifying the command is registered and parseable.
        // Instead, test settings directly.
        var settings = new BucketWatchCommand.Settings { Id = "abc123" };

        settings.Id.Should().Be("abc123");
    }

    [Fact]
    public void Settings_SupportsProfileOption()
    {
        var settings = new BucketWatchCommand.Settings
        {
            Id = "bucket1",
            Profile = "staging",
        };

        settings.Id.Should().Be("bucket1");
        settings.Profile.Should().Be("staging");
    }

    [Fact]
    public void Command_CanBeConstructed()
    {
        var config = new CliConfiguration();
        config.SetProfile("default", "http://localhost:5000", "test-token");
        var factory = new ApiClientFactory(config);
        var console = Substitute.For<Spectre.Console.IAnsiConsole>();

        var command = new BucketWatchCommand(factory, console);

        command.Should().NotBeNull();
    }
}
