using CarbonFiles.Cli.Commands.Files;
using CarbonFiles.Cli.Tests.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.File;

public class FileDeleteCommandTests
{
    [Fact]
    public void WithYesFlag_DeletesFile()
    {
        var (app, handler) = TestClientFactory.CreateApp<FileDeleteCommand>();
        handler.SetupDelete("/api/buckets/bucket1/files/docs%2Freadme.txt");

        var result = app.Run("cmd", "bucket1", "docs/readme.txt", "--yes");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Poof");
        handler.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Delete);
    }

    [Fact]
    public void WithoutYesFlag_NonInteractive_DoesNotDelete()
    {
        var (app, _) = TestClientFactory.CreateApp<FileDeleteCommand>();

        var result = app.Run("cmd", "bucket1", "docs/readme.txt");

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("--yes");
    }
}
