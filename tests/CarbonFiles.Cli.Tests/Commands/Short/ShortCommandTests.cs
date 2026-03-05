using CarbonFiles.Cli.Commands.Short;
using CarbonFiles.Cli.Tests.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Short;

public class ShortCommandTests
{
    [Fact]
    public void ShortDelete_WithYesFlag_Deletes()
    {
        var (app, handler) = TestClientFactory.CreateApp<ShortDeleteCommand>();
        handler.SetupDelete("/api/short/abc123");

        var result = app.Run("cmd", "abc123", "--yes");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Poof");
        handler.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Delete);
    }

    [Fact]
    public void ShortDelete_WithoutYes_NonInteractive_ReturnsError()
    {
        var (app, _) = TestClientFactory.CreateApp<ShortDeleteCommand>();

        var result = app.Run("cmd", "abc123");

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("--yes");
    }
}
