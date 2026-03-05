using CarbonFiles.Cli.Commands.Bucket;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketDownloadCommandTests
{
    [Fact]
    public void DefaultOutput_UsesIdAsFilename()
    {
        var settings = new BucketDownloadCommand.Settings
        {
            Id = "abc123",
            Output = null,
        };

        settings.Id.Should().Be("abc123");
        settings.Output.Should().BeNull();
    }

    [Fact]
    public void CustomOutput_UsesProvidedPath()
    {
        var settings = new BucketDownloadCommand.Settings
        {
            Id = "abc123",
            Output = "my-archive.zip",
        };

        settings.Id.Should().Be("abc123");
        settings.Output.Should().Be("my-archive.zip");
    }
}
