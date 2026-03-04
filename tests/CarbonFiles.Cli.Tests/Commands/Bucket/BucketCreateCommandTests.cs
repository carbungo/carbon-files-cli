using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Bucket;

public class BucketCreateCommandTests
{
    private static (CommandAppTester app, ICarbonFilesApi api) CreateApp()
    {
        var api = Substitute.For<ICarbonFilesApi>();
        var services = new ServiceCollection();
        services.AddSingleton(api);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c => c.AddCommand<BucketCreateCommand>("create"));
        return (app, api);
    }

    [Fact]
    public void ValidName_CreatesBucket()
    {
        var (app, api) = CreateApp();
        api.BucketsPOST(Arg.Any<CreateBucketRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CarbonFiles.Client.Bucket
            {
                Id = "xyz789",
                Name = "my-bucket",
                Owner = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var result = app.Run("create", "my-bucket");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("xyz789");
        result.Output.Should().Contain("my-bucket");
        result.Output.Should().Contain("admin");
        api.Received(1).BucketsPOST(
            Arg.Is<CreateBucketRequest>(r => r.Name == "my-bucket"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WithDescriptionAndExpiry_PassesOptions()
    {
        var (app, api) = CreateApp();
        api.BucketsPOST(Arg.Any<CreateBucketRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CarbonFiles.Client.Bucket
            {
                Id = "xyz789",
                Name = "my-bucket",
                Owner = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var result = app.Run("create", "my-bucket", "-d", "A test bucket", "-e", "7d");

        result.ExitCode.Should().Be(0);
        api.Received(1).BucketsPOST(
            Arg.Is<CreateBucketRequest>(r =>
                r.Name == "my-bucket" &&
                r.Description == "A test bucket" &&
                r.ExpiresIn == "7d"),
            Arg.Any<CancellationToken>());
    }
}
