using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Tests.Infrastructure;
using CarbonFiles.Client.Models;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketListCommandTests
{
    [Fact]
    public void WithBuckets_RendersTable()
    {
        var (app, handler) = TestClientFactory.CreateApp<BucketListCommand>();
        handler.Setup(HttpMethod.Get, "/api/buckets", new PaginatedResponse<Client.Models.Bucket>
        {
            Items = new List<Client.Models.Bucket>
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

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("abc123");
        result.Output.Should().Contain("test-bucket");
        result.Output.Should().Contain("admin");
        result.Output.Should().Contain("Showing 1 of 1");
    }

    [Fact]
    public void Empty_ShowsEmptyMessage()
    {
        var (app, handler) = TestClientFactory.CreateApp<BucketListCommand>();
        handler.Setup(HttpMethod.Get, "/api/buckets", new PaginatedResponse<Client.Models.Bucket>
        {
            Items = new List<Client.Models.Bucket>(),
            Total = 0,
            Limit = 50,
            Offset = 0,
        });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("No buckets found.");
    }
}
