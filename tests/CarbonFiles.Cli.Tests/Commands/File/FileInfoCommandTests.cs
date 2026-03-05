using CarbonFiles.Cli.Commands.Files;
using CarbonFiles.Cli.Tests.Infrastructure;
using CarbonFiles.Client.Models;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.File;

public class FileInfoCommandTests
{
    [Fact]
    public void ValidFile_ShowsDetails()
    {
        var (app, handler) = TestClientFactory.CreateApp<FileInfoCommand>(includeFactory: true);
        handler.Setup(HttpMethod.Get, "/api/buckets/bucket1/files/docs%2Freadme.txt", new BucketFile
        {
            Path = "docs/readme.txt",
            Name = "readme.txt",
            Size = 4096,
            MimeType = "text/plain",
            ShortCode = "abc123",
            ShortUrl = "https://example.com/s/abc123",
            Sha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var result = app.Run("cmd", "bucket1", "docs/readme.txt");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("docs/readme.txt");
        result.Output.Should().Contain("readme.txt");
        result.Output.Should().Contain("text/plain");
        result.Output.Should().Contain("abc123");
        result.Output.Should().Contain("e3b0c44298fc1c14");
    }
}
