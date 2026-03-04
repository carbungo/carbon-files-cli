using CarbonFiles.Cli.Commands.Files;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.File;

public class FileDeleteCommandTests
{
    private static (CommandAppTester app, ICarbonFilesApi api) CreateApp()
    {
        var api = Substitute.For<ICarbonFilesApi>();
        var services = new ServiceCollection();
        services.AddSingleton(api);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<FileDeleteCommand>("cmd"));
        return (app, api);
    }

    [Fact]
    public void WithYesFlag_DeletesFile()
    {
        var (app, api) = CreateApp();
        api.FilesDELETE("bucket1", "docs/readme.txt", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = app.Run("cmd", "bucket1", "docs/readme.txt", "--yes");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Deleted");
        api.Received(1).FilesDELETE("bucket1", "docs/readme.txt", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WithoutYesFlag_NonInteractive_DoesNotDelete()
    {
        var (app, api) = CreateApp();

        var result = app.Run("cmd", "bucket1", "docs/readme.txt");

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("--yes");
        api.DidNotReceive().FilesDELETE(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
