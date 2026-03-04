using CarbonFiles.Cli.Commands.Files;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.File;

public class FileListCommandTests
{
    private static (CommandAppTester app, ICarbonFilesApi api) CreateApp()
    {
        var api = Substitute.For<ICarbonFilesApi>();
        var services = new ServiceCollection();
        services.AddSingleton(api);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<FileListCommand>("cmd"));
        return (app, api);
    }

    [Fact]
    public void WithFiles_RendersTable()
    {
        var (app, api) = CreateApp();
        api.FilesGET(
                "bucket1",
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseOfBucketFile
            {
                Items = new List<BucketFile>
                {
                    new()
                    {
                        Path = "docs/readme.txt",
                        Name = "readme.txt",
                        Size = 2048,
                        MimeType = "text/plain",
                        ShortCode = "abc123",
                        ShortUrl = "https://example.com/s/abc123",
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow,
                    }
                },
                Total = 1,
                Limit = 50,
                Offset = 0,
            });

        var result = app.Run("cmd", "bucket1");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("docs/readme.txt");
        result.Output.Should().Contain("text/plain");
        result.Output.Should().Contain("Showing 1 of 1");
    }

    [Fact]
    public void Empty_ShowsMessage()
    {
        var (app, api) = CreateApp();
        api.FilesGET(
                "bucket1",
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseOfBucketFile
            {
                Items = new List<BucketFile>(),
                Total = 0,
                Limit = 50,
                Offset = 0,
            });

        var result = app.Run("cmd", "bucket1");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("No files found.");
    }

    [Fact]
    public void WithPath_UsesDirectoryListing()
    {
        var (app, api) = CreateApp();
        api.Ls(
                "bucket1",
                "docs/",
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new DirectoryListingResponse
            {
                Folders = new List<string> { "sub/" },
                Files = new List<BucketFile>
                {
                    new()
                    {
                        Path = "docs/readme.txt",
                        Name = "readme.txt",
                        Size = 1024,
                        MimeType = "text/plain",
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow,
                    }
                },
                TotalFiles = 1,
                TotalFolders = 1,
                Limit = 50,
                Offset = 0,
            });

        var result = app.Run("cmd", "bucket1", "--path", "docs/");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("sub/");
        result.Output.Should().Contain("docs/readme.txt");
        result.Output.Should().Contain("folder");
    }
}
