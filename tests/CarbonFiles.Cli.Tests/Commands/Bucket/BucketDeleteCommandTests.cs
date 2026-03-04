using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketDeleteCommandTests
{
    private static (CommandAppTester app, ICarbonFilesApi api) CreateApp()
    {
        var api = Substitute.For<ICarbonFilesApi>();
        var services = new ServiceCollection();
        services.AddSingleton(api);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<BucketDeleteCommand>("delete"));
        return (app, api);
    }

    [Fact]
    public void WithYesFlag_DeletesBucket()
    {
        var (app, api) = CreateApp();
        api.BucketsDELETE("abc123", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = app.Run("delete", "abc123", "--yes");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Deleted");
        api.Received(1).BucketsDELETE("abc123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WithoutYesFlag_NonInteractive_DoesNotDelete()
    {
        var (app, api) = CreateApp();

        var result = app.Run("delete", "abc123");

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("--yes");
        api.DidNotReceive().BucketsDELETE(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
