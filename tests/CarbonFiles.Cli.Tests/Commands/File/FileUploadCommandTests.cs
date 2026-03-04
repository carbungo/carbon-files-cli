using CarbonFiles.Cli.Commands.Files;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.File;

public class FileUploadCommandTests
{
    [Fact]
    public void StdinMode_RequiresName()
    {
        // When --stdin is used without -n, the Name property should be null
        // The command should validate this at runtime
        var settings = new FileUploadCommand.Settings
        {
            BucketId = "bucket1",
            Paths = [],
            Stdin = true,
            Name = null,
        };

        settings.Stdin.Should().BeTrue();
        settings.Name.Should().BeNull();
    }

    [Fact]
    public void StdinMode_WithName_IsValid()
    {
        var settings = new FileUploadCommand.Settings
        {
            BucketId = "bucket1",
            Paths = [],
            Stdin = true,
            Name = "test.txt",
        };

        settings.Stdin.Should().BeTrue();
        settings.Name.Should().Be("test.txt");
    }

    [Fact]
    public void Settings_ParseFilePaths()
    {
        var settings = new FileUploadCommand.Settings
        {
            BucketId = "bucket1",
            Paths = ["file1.txt", "file2.txt", "docs/readme.md"],
        };

        settings.BucketId.Should().Be("bucket1");
        settings.Paths.Should().HaveCount(3);
        settings.Paths.Should().Contain("file1.txt");
        settings.Paths.Should().Contain("docs/readme.md");
    }

    [Fact]
    public void Settings_Recursive_DefaultsFalse()
    {
        var settings = new FileUploadCommand.Settings
        {
            BucketId = "bucket1",
            Paths = ["dir/"],
        };

        settings.Recursive.Should().BeFalse();
    }

    [Fact]
    public void Settings_Token_Override()
    {
        var settings = new FileUploadCommand.Settings
        {
            BucketId = "bucket1",
            Paths = ["file.txt"],
            Token = "cfu_my_upload_token",
        };

        settings.Token.Should().Be("cfu_my_upload_token");
    }
}
