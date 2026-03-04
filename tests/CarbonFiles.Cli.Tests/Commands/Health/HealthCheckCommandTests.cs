using CarbonFiles.Cli.Commands.Health;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Health;

public class HealthCheckCommandTests
{
    private static (CommandAppTester app, ICarbonFilesApi api) CreateApp<T>() where T : class, ICommand
    {
        var api = Substitute.For<ICarbonFilesApi>();
        var services = new ServiceCollection();
        services.AddSingleton(api);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<T>("cmd"));
        return (app, api);
    }

    [Fact]
    public void Health_Healthy_ShowsGreen()
    {
        var (app, api) = CreateApp<HealthCheckCommand>();
        api.Healthz(Arg.Any<CancellationToken>())
            .Returns(new HealthResponse
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
        var (app, api) = CreateApp<HealthCheckCommand>();
        api.Healthz(Arg.Any<CancellationToken>())
            .Returns(new HealthResponse
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
