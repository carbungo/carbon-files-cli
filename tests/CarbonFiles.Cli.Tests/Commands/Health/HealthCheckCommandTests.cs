using CarbonFiles.Cli.Commands.Health;
using CarbonFiles.Cli.Tests.Infrastructure;
using CarbonFiles.Client.Models;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Health;

public class HealthCheckCommandTests
{
    [Fact]
    public void Health_Healthy_ShowsGreen()
    {
        var (app, handler) = TestClientFactory.CreateApp<HealthCheckCommand>();
        handler.Setup(HttpMethod.Get, "/healthz", new HealthResponse
        {
            Status = "Healthy",
            UptimeSeconds = 86400,
            Db = "ok",
        });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Healthy");
        result.Output.Should().Contain("ok");
    }

    [Fact]
    public void Health_Unhealthy_ShowsRed()
    {
        var (app, handler) = TestClientFactory.CreateApp<HealthCheckCommand>();
        handler.Setup(HttpMethod.Get, "/healthz", new HealthResponse
        {
            Status = "Unhealthy",
            UptimeSeconds = 3661,
            Db = "error",
        });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Unhealthy");
        result.Output.Should().Contain("error");
    }
}
