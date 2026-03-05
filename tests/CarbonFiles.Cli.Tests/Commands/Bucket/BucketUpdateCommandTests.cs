using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Tests.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketUpdateCommandTests
{
    [Fact]
    public void WithNameChange_UpdatesBucket()
    {
        var (app, handler) = TestClientFactory.CreateApp<BucketUpdateCommand>();
        handler.Setup(HttpMethod.Patch, "/api/buckets/abc123", new Client.Models.Bucket
        {
            Id = "abc123",
            Name = "new-name",
            Owner = "admin",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var result = app.Run("cmd", "abc123", "--name", "new-name");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Bucket updated");
        result.Output.Should().Contain("abc123");
        result.Output.Should().Contain("new-name");
    }

    [Fact]
    public void NoOptions_ShowsError()
    {
        var (app, _) = TestClientFactory.CreateApp<BucketUpdateCommand>();

        var result = app.Run("cmd", "abc123");

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("At least one of --name, --description, or --expires must be provided");
    }
}
