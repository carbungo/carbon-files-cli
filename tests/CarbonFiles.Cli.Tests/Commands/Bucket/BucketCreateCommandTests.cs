using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Tests.Infrastructure;
using CarbonFiles.Client.Models;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketCreateCommandTests
{
    [Fact]
    public void ValidName_CreatesBucket()
    {
        var (app, handler) = TestClientFactory.CreateApp<BucketCreateCommand>(includeFactory: true);
        handler.Setup(HttpMethod.Post, "/api/buckets", new Client.Models.Bucket
        {
            Id = "xyz789",
            Name = "my-bucket",
            Owner = "admin",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var result = app.Run("cmd", "my-bucket");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("xyz789");
        result.Output.Should().Contain("my-bucket");
        result.Output.Should().Contain("admin");
    }

    [Fact]
    public void WithDescriptionAndExpiry_PassesOptions()
    {
        var (app, handler) = TestClientFactory.CreateApp<BucketCreateCommand>(includeFactory: true);
        handler.Setup(HttpMethod.Post, "/api/buckets", new Client.Models.Bucket
        {
            Id = "xyz789",
            Name = "my-bucket",
            Owner = "admin",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var result = app.Run("cmd", "my-bucket", "-d", "A test bucket", "-e", "7d");

        result.ExitCode.Should().Be(0);
        // Verify the POST was made
        handler.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Post);
    }
}
