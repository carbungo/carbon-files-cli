using CarbonFiles.Cli.Commands.Files;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.File;

public class FileDownloadCommandTests
{
    [Fact]
    public void Settings_DefaultOutput_UsesFilename()
    {
        var settings = new FileDownloadCommand.Settings
        {
            BucketId = "bucket1",
            Path = "docs/readme.txt",
            Output = null,
        };

        settings.BucketId.Should().Be("bucket1");
        settings.Path.Should().Be("docs/readme.txt");
        // Default output should be null, command will resolve to filename portion
        settings.Output.Should().BeNull();
    }

    [Fact]
    public void Settings_CustomOutput()
    {
        var settings = new FileDownloadCommand.Settings
        {
            BucketId = "bucket1",
            Path = "docs/readme.txt",
            Output = "/tmp/my-file.txt",
        };

        settings.Output.Should().Be("/tmp/my-file.txt");
    }

    [Fact]
    public void Settings_Path_IsRequired()
    {
        var settings = new FileDownloadCommand.Settings
        {
            BucketId = "bucket1",
            Path = "images/logo.png",
        };

        settings.Path.Should().Be("images/logo.png");
    }
}
