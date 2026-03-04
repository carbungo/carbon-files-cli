using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketInfoCommandTests
{
    private static (CommandAppTester app, ICarbonFilesApi api) CreateApp()
    {
        var api = Substitute.For<ICarbonFilesApi>();
        var config = new CliConfiguration();
        config.SetProfile("default", "http://localhost", "test-token");
        var services = new ServiceCollection();
        services.AddSingleton(api);
        services.AddSingleton(new ApiClientFactory(config));
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<BucketInfoCommand>("info"));
        return (app, api);
    }

    [Fact]
    public void ValidId_ShowsDetails()
    {
        var (app, api) = CreateApp();
        api.BucketsGET2("abc123", Arg.Any<CancellationToken>())
            .Returns(new BucketDetailResponse
            {
                Id = "abc123",
                Name = "test-bucket",
                Owner = "admin",
                Description = "My test bucket",
                FileCount = 3,
                TotalSize = 2048,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = null,
                Files = new List<BucketFile>
                {
                    new()
                    {
                        Path = "docs/readme.txt",
                        Name = "readme.txt",
                        Size = 1024,
                        MimeType = "text/plain",
                        ShortUrl = "https://example.com/s/abc",
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow,
                    }
                },
                HasMoreFiles = false,
            });

        var result = app.Run("info", "abc123");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("abc123");
        result.Output.Should().Contain("test-bucket");
        result.Output.Should().Contain("admin");
        result.Output.Should().Contain("My test bucket");
        result.Output.Should().Contain("docs/readme.txt");
        result.Output.Should().Contain("text/plain");
    }

    [Fact]
    public void WithHasMoreFiles_ShowsHint()
    {
        var (app, api) = CreateApp();
        api.BucketsGET2("abc123", Arg.Any<CancellationToken>())
            .Returns(new BucketDetailResponse
            {
                Id = "abc123",
                Name = "test-bucket",
                Owner = "admin",
                FileCount = 100,
                TotalSize = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                Files = new List<BucketFile>(),
                HasMoreFiles = true,
            });

        var result = app.Run("info", "abc123");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("cf file list abc123");
    }
}
