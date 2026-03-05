using CarbonFiles.Cli.Commands.Key;
using CarbonFiles.Cli.Tests.Infrastructure;
using CarbonFiles.Client.Models;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Commands.Key;

public class KeyCommandTests
{
    [Fact]
    public void KeyList_WithKeys_RendersTable()
    {
        var (app, handler) = TestClientFactory.CreateApp<KeyListCommand>();
        handler.Setup(HttpMethod.Get, "/api/keys", new PaginatedResponse<ApiKeyListItem>
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
        var (app, handler) = TestClientFactory.CreateApp<KeyListCommand>();
        handler.Setup(HttpMethod.Get, "/api/keys", new PaginatedResponse<ApiKeyListItem>
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
        var (app, handler) = TestClientFactory.CreateApp<KeyCreateCommand>();
        handler.Setup(HttpMethod.Post, "/api/keys", new ApiKeyResponse
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
        var (app, handler) = TestClientFactory.CreateApp<KeyDeleteCommand>();
        handler.SetupDelete("/api/keys/cf4_abcd1234");

        var result = app.Run("cmd", "cf4_abcd1234", "--yes");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Deleted");
        handler.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Delete);
    }

    [Fact]
    public void KeyUsage_ValidPrefix_ShowsStats()
    {
        var (app, handler) = TestClientFactory.CreateApp<KeyUsageCommand>();
        handler.Setup(HttpMethod.Get, "/api/keys/cf4_abcd1234/usage", new ApiKeyUsageResponse
        {
            Prefix = "cf4_abcd1234",
            Name = "deploy-key",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUsedAt = DateTimeOffset.UtcNow,
            BucketCount = 2,
            FileCount = 10,
            TotalSize = 4096,
            TotalDownloads = 50,
            Buckets = new List<Client.Models.Bucket>
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
