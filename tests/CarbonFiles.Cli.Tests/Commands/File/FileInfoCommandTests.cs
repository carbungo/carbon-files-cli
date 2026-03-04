using CarbonFiles.Cli.Commands.Files;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.File;

public class FileInfoCommandTests
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
        app.Configure(c => c.AddCommand<FileInfoCommand>("cmd"));
        return (app, api);
    }

    [Fact]
    public void ValidFile_ShowsDetails()
    {
        var (app, api) = CreateApp();
        api.FilesGET2("bucket1", "docs/readme.txt", Arg.Any<CancellationToken>())
            .Returns(new BucketFile
            {
                Path = "docs/readme.txt",
                Name = "readme.txt",
                Size = 4096,
                MimeType = "text/plain",
                ShortCode = "abc123",
                ShortUrl = "https://example.com/s/abc123",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });

        var result = app.Run("cmd", "bucket1", "docs/readme.txt");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("docs/readme.txt");
        result.Output.Should().Contain("readme.txt");
        result.Output.Should().Contain("text/plain");
        result.Output.Should().Contain("abc123");
    }
}
