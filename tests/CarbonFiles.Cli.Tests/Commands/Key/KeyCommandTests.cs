using CarbonFiles.Cli.Commands.Key;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Key;

public class KeyCommandTests
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
    public void KeyList_WithKeys_RendersTable()
    {
        var (app, api) = CreateApp<KeyListCommand>();
        api.KeysGET(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseOfApiKeyListItem
            {
                Items = new List<ApiKeyListItem>
                {
                    new()
                    {
                        Prefix = "cf4_abcd1234",
                        Name = "deploy-key",
                        CreatedAt = DateTimeOffset.UtcNow,
                        LastUsedAt = null,
                        BucketCount = 3,
                        FileCount = 12,
                        TotalSize = 2048,
                    }
                },
                Total = 1,
                Limit = 50,
                Offset = 0,
            });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("cf4_abcd1234");
        result.Output.Should().Contain("deploy-key");
        result.Output.Should().Contain("Showing 1 of 1");
    }

    [Fact]
    public void KeyList_Empty_ShowsMessage()
    {
        var (app, api) = CreateApp<KeyListCommand>();
        api.KeysGET(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseOfApiKeyListItem
            {
                Items = new List<ApiKeyListItem>(),
                Total = 0,
                Limit = 50,
                Offset = 0,
            });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("No API keys found.");
    }

    [Fact]
    public void KeyCreate_ValidName_CreatesKey()
    {
        var (app, api) = CreateApp<KeyCreateCommand>();
        api.KeysPOST(Arg.Any<CreateApiKeyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ApiKeyResponse
            {
                Key = "cf4_abcd1234_full_secret_here",
                Prefix = "cf4_abcd1234",
                Name = "deploy-key",
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var result = app.Run("cmd", "deploy-key");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("cf4_abcd1234_full_secret_here");
        result.Output.Should().Contain("cf4_abcd1234");
        result.Output.Should().Contain("deploy-key");
        result.Output.Should().Contain("Save this key");
    }

    [Fact]
    public void KeyDelete_WithYesFlag_DeletesKey()
    {
        var (app, api) = CreateApp<KeyDeleteCommand>();
        api.KeysDELETE("cf4_abcd1234", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = app.Run("cmd", "cf4_abcd1234", "--yes");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Deleted");
        api.Received(1).KeysDELETE("cf4_abcd1234", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void KeyUsage_ValidPrefix_ShowsStats()
    {
        var (app, api) = CreateApp<KeyUsageCommand>();
        api.Usage("cf4_abcd1234", Arg.Any<CancellationToken>())
            .Returns(new ApiKeyUsageResponse
            {
                Prefix = "cf4_abcd1234",
                Name = "deploy-key",
                CreatedAt = DateTimeOffset.UtcNow,
                LastUsedAt = DateTimeOffset.UtcNow,
                BucketCount = 2,
                FileCount = 10,
                TotalSize = 4096,
                TotalDownloads = 50,
                Buckets = new List<CarbonFiles.Client.Bucket>
                {
                    new()
                    {
                        Id = "bkt001",
                        Name = "my-bucket",
                        Owner = "deploy-key",
                        FileCount = 10,
                        TotalSize = 4096,
                        CreatedAt = DateTimeOffset.UtcNow,
                    }
                },
            });

        var result = app.Run("cmd", "cf4_abcd1234");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("cf4_abcd1234");
        result.Output.Should().Contain("deploy-key");
        result.Output.Should().Contain("bkt001");
        result.Output.Should().Contain("my-bucket");
    }
}
