using CarbonFiles.Cli.Commands.Files;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.File;

public class FileUploadCommandTests
{
    [Fact]
    public void StdinMode_RequiresName()
    {
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

    [Fact]
    public void Settings_Flat_DefaultsFalse()
    {
        var settings = new FileUploadCommand.Settings
        {
            BucketId = "bucket1",
            Paths = ["file.txt"],
        };

        settings.Flat.Should().BeFalse();
    }

    [Theory]
    [InlineData("/home/user/project/src/utils/helper.cs", "/home/user/project", false, "src/utils/helper.cs")]
    [InlineData("/home/user/project/src/main.cs", "/home/user/project", false, "src/main.cs")]
    [InlineData("/home/user/project/readme.md", "/home/user/project", false, "readme.md")]
    [InlineData("/tmp/other/file.txt", "/home/user/project", false, "file.txt")] // outside base dir → filename only
    [InlineData("/home/user/project/src/utils/helper.cs", "/home/user/project", true, "helper.cs")] // flat mode
    [InlineData("/home/user/project/docs/guide.md", "/home/user/project", true, "guide.md")] // flat mode
    public void ComputeRemotePath_ResolvesCorrectly(string fullPath, string baseDir, bool flat, string expected)
    {
        var result = FileUploadCommand.ComputeRemotePath(fullPath, baseDir, flat);
        result.Should().Be(expected);
    }
}
