using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketUpdateCommandTests
{
    private static (CommandAppTester app, ICarbonFilesApi api) CreateApp()
    {
        var api = Substitute.For<ICarbonFilesApi>();
        var services = new ServiceCollection();
        services.AddSingleton(api);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<BucketUpdateCommand>("update"));
        return (app, api);
    }

    [Fact]
    public void WithNameChange_UpdatesBucket()
    {
        var (app, api) = CreateApp();
        api.BucketsPATCH("abc123", Arg.Any<UpdateBucketRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CarbonFiles.Client.Bucket
            {
                Id = "abc123",
                Name = "new-name",
                Owner = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var result = app.Run("update", "abc123", "--name", "new-name");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Bucket Updated");
        result.Output.Should().Contain("abc123");
        result.Output.Should().Contain("new-name");
        api.Received(1).BucketsPATCH(
            "abc123",
            Arg.Is<UpdateBucketRequest>(r => r.Name == "new-name"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WithAllOptions_PassesAllFields()
    {
        var (app, api) = CreateApp();
        api.BucketsPATCH("abc123", Arg.Any<UpdateBucketRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CarbonFiles.Client.Bucket
            {
                Id = "abc123",
                Name = "updated",
                Owner = "admin",
                Description = "new desc",
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var result = app.Run("update", "abc123", "--name", "updated", "--description", "new desc", "--expires", "7d");

        result.ExitCode.Should().Be(0);
        api.Received(1).BucketsPATCH(
            "abc123",
            Arg.Is<UpdateBucketRequest>(r =>
                r.Name == "updated" &&
                r.Description == "new desc" &&
                r.ExpiresIn == "7d"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void NoOptions_ShowsError()
    {
        var (app, api) = CreateApp();

        var result = app.Run("update", "abc123");

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("At least one of --name, --description, or --expires must be provided");
        api.DidNotReceive().BucketsPATCH(
            Arg.Any<string>(),
            Arg.Any<UpdateBucketRequest>(),
            Arg.Any<CancellationToken>());
    }
}
