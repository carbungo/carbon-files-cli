using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Tests.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketDeleteCommandTests
{
    [Fact]
    public void WithYesFlag_DeletesBucket()
    {
        var (app, handler) = TestClientFactory.CreateApp<BucketDeleteCommand>();
        handler.SetupDelete("/api/buckets/abc123");

        var result = app.Run("cmd", "abc123", "--yes");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Deleted");
        handler.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Delete);
    }

    [Fact]
    public void WithoutYesFlag_NonInteractive_DoesNotDelete()
    {
        var (app, _) = TestClientFactory.CreateApp<BucketDeleteCommand>();

        var result = app.Run("cmd", "abc123");

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("--yes");
    }
}
