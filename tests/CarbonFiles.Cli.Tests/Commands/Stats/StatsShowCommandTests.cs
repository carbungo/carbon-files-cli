using CarbonFiles.Cli.Commands.Stats;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Stats;

public class StatsShowCommandTests
{
    private static (CommandAppTester app, ICarbonFilesApi api) CreateApp<T>() where T : class, ICommand
    {
        var api = Substitute.For<ICarbonFilesApi>();
        var services = new ServiceCollection();
        services.AddSingleton(api);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<T>("cmd"));
        return (app, api);
    }

    [Fact]
    public void Stats_ShowsStats()
    {
        var (app, api) = CreateApp<StatsShowCommand>();
        api.Stats(Arg.Any<CancellationToken>())
            .Returns(new StatsResponse
            {
                TotalBuckets = 5,
                TotalFiles = 42,
                TotalSize = 1048576,
                TotalKeys = 3,
                TotalDownloads = 100,
                StorageByOwner = new List<OwnerStats>
                {
                    new()
                    {
                        Owner = "admin",
                        BucketCount = 3,
                        FileCount = 30,
                        TotalSize = 524288,
                    },
                    new()
                    {
                        Owner = "deploy-key",
                        BucketCount = 2,
                        FileCount = 12,
                        TotalSize = 524288,
                    }
                }
            });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("5");
        result.Output.Should().Contain("42");
        result.Output.Should().Contain("3");
        result.Output.Should().Contain("100");
        result.Output.Should().Contain("admin");
        result.Output.Should().Contain("deploy-key");
    }

    [Fact]
    public void Stats_NoOwners_ShowsSummaryOnly()
    {
        var (app, api) = CreateApp<StatsShowCommand>();
        api.Stats(Arg.Any<CancellationToken>())
            .Returns(new StatsResponse
            {
                TotalBuckets = 0,
                TotalFiles = 0,
                TotalSize = 0,
                TotalKeys = 0,
                TotalDownloads = 0,
                StorageByOwner = new List<OwnerStats>()
            });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("0");
    }
}
