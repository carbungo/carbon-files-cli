using CarbonFiles.Cli.Commands.Files;
using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;
using NSubstitute;
using Spectre.Console;
using IO = System.IO;

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

    [Fact]
    public void ResolveFilePaths_Directory_PreservesRelativePaths()
    {
        // Arrange: create a temp directory structure
        var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var docsDir = Path.Combine(tempRoot, "docs");
        var subDir = Path.Combine(docsDir, "subdir");
        Directory.CreateDirectory(subDir);
        IO.File.WriteAllText(Path.Combine(docsDir, "doc1.txt"), "content");
        IO.File.WriteAllText(Path.Combine(subDir, "doc2.txt"), "content");

        try
        {
            var console = Substitute.For<IAnsiConsole>();
            var config = new CliConfiguration();
            config.SetProfile("default", "http://localhost", "token");
            var factory = new ApiClientFactory(config);
            var command = new FileUploadCommand(factory, console);

            var settings = new FileUploadCommand.Settings
            {
                BucketId = "bucket1",
                Paths = [docsDir],
                Recursive = true,
            };

            var files = command.ResolveFilePaths(settings);

            // Remote paths should be docs/doc1.txt and docs/subdir/doc2.txt,
            // not just doc1.txt and doc2.txt
            files.Should().HaveCount(2);
            files.Select(f => f.RemotePath).Should().Contain("docs/doc1.txt");
            files.Select(f => f.RemotePath).Should().Contain("docs/subdir/doc2.txt");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveFilePaths_Directory_WithExplicitBaseDir_UsesBaseDir()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var docsDir = Path.Combine(tempRoot, "docs");
        Directory.CreateDirectory(docsDir);
        IO.File.WriteAllText(Path.Combine(docsDir, "doc1.txt"), "content");

        try
        {
            var console = Substitute.For<IAnsiConsole>();
            var config = new CliConfiguration();
            config.SetProfile("default", "http://localhost", "token");
            var factory = new ApiClientFactory(config);
            var command = new FileUploadCommand(factory, console);

            var settings = new FileUploadCommand.Settings
            {
                BucketId = "bucket1",
                Paths = [docsDir],
                Recursive = true,
                BaseDir = docsDir, // explicit base-dir = the docs dir itself → file at root
            };

            var files = command.ResolveFilePaths(settings);

            files.Should().ContainSingle();
            files[0].RemotePath.Should().Be("doc1.txt");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveFilePaths_Directory_FlatMode_StripsAllPaths()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var docsDir = Path.Combine(tempRoot, "docs");
        var subDir = Path.Combine(docsDir, "sub");
        Directory.CreateDirectory(subDir);
        IO.File.WriteAllText(Path.Combine(docsDir, "doc1.txt"), "content");
        IO.File.WriteAllText(Path.Combine(subDir, "doc2.txt"), "content");

        try
        {
            var console = Substitute.For<IAnsiConsole>();
            var config = new CliConfiguration();
            config.SetProfile("default", "http://localhost", "token");
            var factory = new ApiClientFactory(config);
            var command = new FileUploadCommand(factory, console);

            var settings = new FileUploadCommand.Settings
            {
                BucketId = "bucket1",
                Paths = [docsDir],
                Recursive = true,
                Flat = true,
            };

            var files = command.ResolveFilePaths(settings);

            files.Should().HaveCount(2);
            files.Select(f => f.RemotePath).Should().Contain("doc1.txt");
            files.Select(f => f.RemotePath).Should().Contain("doc2.txt");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
