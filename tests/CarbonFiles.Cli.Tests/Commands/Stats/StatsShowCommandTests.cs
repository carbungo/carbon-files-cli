using CarbonFiles.Cli.Commands.Stats;
using CarbonFiles.Cli.Tests.Infrastructure;
using CarbonFiles.Client.Models;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Stats;

public class StatsShowCommandTests
{
    [Fact]
    public void Stats_ShowsStats()
    {
        var (app, handler) = TestClientFactory.CreateApp<StatsShowCommand>();
        handler.Setup(HttpMethod.Get, "/api/stats", new StatsResponse
        {
            TotalBuckets = 5,
            TotalFiles = 42,
            TotalSize = 1048576,
            TotalKeys = 3,
            TotalDownloads = 100,
            StorageByOwner = new List<OwnerStats>
            {
                new()
                {
                    Owner = "admin",
                    BucketCount = 3,
                    FileCount = 30,
                    TotalSize = 524288,
                },
                new()
                {
                    Owner = "deploy-key",
                    BucketCount = 2,
                    FileCount = 12,
                    TotalSize = 524288,
                }
            }
        });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("5");
        result.Output.Should().Contain("42");
        result.Output.Should().Contain("3");
        result.Output.Should().Contain("100");
        result.Output.Should().Contain("admin");
        result.Output.Should().Contain("deploy-key");
    }

    [Fact]
    public void Stats_NoOwners_ShowsSummaryOnly()
    {
        var (app, handler) = TestClientFactory.CreateApp<StatsShowCommand>();
        handler.Setup(HttpMethod.Get, "/api/stats", new StatsResponse
        {
            TotalBuckets = 0,
            TotalFiles = 0,
            TotalSize = 0,
            TotalKeys = 0,
            TotalDownloads = 0,
            StorageByOwner = new List<OwnerStats>()
        });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("0");
    }
}
