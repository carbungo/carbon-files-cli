using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Tests.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketSummaryCommandTests
{
    [Fact]
    public void ValidId_PrintsSummaryText()
    {
        var (app, handler) = TestClientFactory.CreateApp<BucketSummaryCommand>();
        handler.SetupText(HttpMethod.Get, "/api/buckets/abc123/summary",
            "Bucket: my-bucket\nFiles: 3\nTotal size: 4.2 KB");

        var result = app.Run("cmd", "abc123");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("my-bucket");
        result.Output.Should().Contain("Files: 3");
        result.Output.Should().Contain("4.2 KB");
    }

    [Fact]
    public void HitsCorrectEndpoint()
    {
        var (app, handler) = TestClientFactory.CreateApp<BucketSummaryCommand>();
        handler.SetupText(HttpMethod.Get, "/api/buckets/xyz/summary", "summary text");

        app.Run("cmd", "xyz");

        handler.Requests.Should().ContainSingle(r =>
            r.RequestUri!.PathAndQuery.Contains("/api/buckets/xyz/summary"));
    }
}
