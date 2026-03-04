using CarbonFiles.Cli.Commands.Short;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Short;

public class ShortCommandTests
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
    public void ShortDelete_WithYesFlag_Deletes()
    {
        var (app, api) = CreateApp<ShortDeleteCommand>();
        api.Short("abc123", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = app.Run("cmd", "abc123", "--yes");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Deleted");
        api.Received(1).Short("abc123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ShortDelete_WithoutYes_NonInteractive_ReturnsError()
    {
        var (app, api) = CreateApp<ShortDeleteCommand>();

        var result = app.Run("cmd", "abc123");

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("--yes");
    }
}
