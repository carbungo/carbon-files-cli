using CarbonFiles.Cli.Commands.Files;
using CarbonFiles.Cli.Tests.Infrastructure;
using CarbonFiles.Client.Models;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.File;

public class FileListCommandTests
{
    [Fact]
    public void WithFiles_RendersTable()
    {
        var (app, handler) = TestClientFactory.CreateApp<FileListCommand>();
        handler.Setup(HttpMethod.Get, "/api/buckets/bucket1/files", new PaginatedResponse<BucketFile>
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
        var (app, handler) = TestClientFactory.CreateApp<FileListCommand>();
        handler.Setup(HttpMethod.Get, "/api/buckets/bucket1/files", new PaginatedResponse<BucketFile>
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
        var (app, handler) = TestClientFactory.CreateApp<FileListCommand>();
        handler.Setup(HttpMethod.Get, "/api/buckets/bucket1/ls", new DirectoryListingResponse
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
