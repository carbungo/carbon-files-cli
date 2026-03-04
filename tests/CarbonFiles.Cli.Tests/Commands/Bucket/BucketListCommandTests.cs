using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketListCommandTests
{
    private static (CommandAppTester app, ICarbonFilesApi api) CreateApp()
    {
        var api = Substitute.For<ICarbonFilesApi>();
        var services = new ServiceCollection();
        services.AddSingleton(api);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<BucketListCommand>("list"));
        return (app, api);
    }

    [Fact]
    public void WithBuckets_RendersTable()
    {
        var (app, api) = CreateApp();
        api.BucketsGET(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseOfBucket
            {
                Items = new List<CarbonFiles.Client.Bucket>
                {
                    new()
                    {
                        Id = "abc123",
                        Name = "test-bucket",
                        Owner = "admin",
                        FileCount = 5,
                        TotalSize = 1048576,
                        CreatedAt = DateTimeOffset.UtcNow,
                        ExpiresAt = null,
                    }
                },
                Total = 1,
                Limit = 50,
                Offset = 0,
            });

        var result = app.Run("list");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("abc123");
        result.Output.Should().Contain("test-bucket");
        result.Output.Should().Contain("admin");
        result.Output.Should().Contain("Showing 1 of 1");
    }

    [Fact]
    public void Empty_ShowsEmptyMessage()
    {
        var (app, api) = CreateApp();
        api.BucketsGET(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseOfBucket
            {
                Items = new List<CarbonFiles.Client.Bucket>(),
                Total = 0,
                Limit = 50,
                Offset = 0,
            });

        var result = app.Run("list");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("No buckets found.");
    }
}
