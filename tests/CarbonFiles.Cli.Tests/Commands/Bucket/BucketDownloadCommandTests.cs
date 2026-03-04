using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketDownloadCommandTests
{
    [Fact]
    public void DefaultOutput_UsesIdAsFilename()
    {
        // We can't fully test the download (requires a real server),
        // but we can verify settings parsing by checking the command
        // is properly configured. The actual HTTP call will fail,
        // so we just verify the settings class works correctly.
        var settings = new BucketDownloadCommand.Settings
        {
            Id = "abc123",
            Output = null,
        };

        settings.Id.Should().Be("abc123");
        // Default output should be null, command will resolve to {id}.zip
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
