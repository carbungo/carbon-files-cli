using CarbonFiles.Cli.Commands.Token;
using CarbonFiles.Cli.Tests.Infrastructure;
using CarbonFiles.Client.Models;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Token;

public class TokenCommandTests
{
    [Fact]
    public void CreateUploadToken_ValidBucket_ShowsToken()
    {
        var (app, handler) = TestClientFactory.CreateApp<TokenCreateUploadCommand>(includeFactory: true);
        handler.Setup(HttpMethod.Post, "/api/buckets/bkt001/tokens", new UploadTokenResponse
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
    }

    [Fact]
    public void CreateDashboardToken_ShowsToken()
    {
        var (app, handler) = TestClientFactory.CreateApp<TokenCreateDashboardCommand>();
        handler.Setup(HttpMethod.Post, "/api/tokens/dashboard", new DashboardTokenResponse
        {
            Token = "eyJhbGciOiJIUzI1NiJ9.test",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
        });

        var result = app.Run("cmd", "--expires", "24h");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("eyJhbGciOiJIUzI1NiJ9.test");
    }

    [Fact]
    public void TokenInfo_ShowsDetails()
    {
        var (app, handler) = TestClientFactory.CreateApp<TokenInfoCommand>();
        handler.Setup(HttpMethod.Get, "/api/tokens/dashboard/me", new DashboardTokenInfo
        {
            Scope = "admin",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(12),
        });

        var result = app.Run("cmd");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("admin");
    }
}
