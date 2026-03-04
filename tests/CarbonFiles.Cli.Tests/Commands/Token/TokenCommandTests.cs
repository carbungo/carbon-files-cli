using CarbonFiles.Cli.Commands.Token;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Token;

public class TokenCommandTests
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
    public void CreateUploadToken_ValidBucket_ShowsToken()
    {
        var (app, api) = CreateApp<TokenCreateUploadCommand>();
        api.Tokens("bkt001", Arg.Any<CreateUploadTokenRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UploadTokenResponse
            {
                Token = "cfu_test_upload_token",
                BucketId = "bkt001",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                MaxUploads = 10,
                UploadsUsed = 0,
            });

        var result = app.Run("cmd", "bkt001", "--expires", "1h", "--max-uploads", "10");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("cfu_test_upload_token");
        result.Output.Should().Contain("bkt001");
        api.Received(1).Tokens("bkt001", Arg.Any<CreateUploadTokenRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateDashboardToken_ShowsToken()
    {
        var (app, api) = CreateApp<TokenCreateDashboardCommand>();
        api.Dashboard(Arg.Any<CreateDashboardTokenRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DashboardTokenResponse
            {
                Token = "eyJhbGciOiJIUzI1NiJ9.test",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            });

        var result = app.Run("cmd", "--expires", "24h");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("eyJhbGciOiJIUzI1NiJ9.test");
        api.Received(1).Dashboard(Arg.Any<CreateDashboardTokenRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void TokenInfo_ShowsDetails()
    {
        var (app, api) = CreateApp<TokenInfoCommand>();
        api.Me(Arg.Any<CancellationToken>())
            .Returns(new DashboardTokenInfo
            {
                Scope = "admin",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(12),
            });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("admin");
        api.Received(1).Me(Arg.Any<CancellationToken>());
    }
}
