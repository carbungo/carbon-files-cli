using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Tests.Infrastructure;
using CarbonFiles.Client.Models;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketInfoCommandTests
{
    [Fact]
    public void ValidId_ShowsDetails()
    {
        var (app, handler) = TestClientFactory.CreateApp<BucketInfoCommand>(includeFactory: true);
        handler.Setup(HttpMethod.Get, "/api/buckets/abc123", new BucketDetailResponse
        {
            Id = "abc123",
            Name = "test-bucket",
            Owner = "admin",
            Description = "My test bucket",
            FileCount = 3,
            TotalSize = 2048,
            UniqueContentCount = 2,
            UniqueContentSize = 1024,
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

        var result = app.Run("cmd", "abc123");

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
        var (app, handler) = TestClientFactory.CreateApp<BucketInfoCommand>(includeFactory: true);
        handler.Setup(HttpMethod.Get, "/api/buckets/abc123", new BucketDetailResponse
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

        var result = app.Run("cmd", "abc123");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("cf file list abc123");
    }
}
