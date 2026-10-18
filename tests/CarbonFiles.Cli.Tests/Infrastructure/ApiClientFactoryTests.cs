using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Infrastructure;

public class ApiClientFactoryTests : IDisposable
{
    private readonly string _tempDir;

    public ApiClientFactoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cf_test_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Create_WithValidProfile_ReturnsClient()
    {
        var configPath = Path.Combine(_tempDir, "config.json");
        var config = CliConfiguration.Load(configPath);
        config.SetProfile("default", "https://example.com", "cf4_test_token");
        config.Save();

        var factory = new ApiClientFactory(config);
        var client = factory.Create();

        client.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithNoProfile_ThrowsWithHelpfulMessage()
    {
        var configPath = Path.Combine(_tempDir, "config.json");
        var config = CliConfiguration.Load(configPath);

        var factory = new ApiClientFactory(config);

        var act = () => factory.Create();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cf config set*");
    }

    [Fact]
    public void Create_WithProfileOverride_UsesOverrideProfile()
    {
        var configPath = Path.Combine(_tempDir, "config.json");
        var config = CliConfiguration.Load(configPath);
        config.SetProfile("default", "https://default.example.com", "token1");
        config.SetProfile("staging", "https://staging.example.com", "token2");
        config.Save();

        var factory = new ApiClientFactory(config);
        var client = factory.Create("staging");

        client.Should().NotBeNull();
    }

    [Fact]
    public void CreateHttpClient_HasInfiniteTimeout()
    {
        // The default 100-second HttpClient.Timeout causes large file uploads to fail mid-stream
        // with "Error while copying content to a stream". Verify that the factory disables it.
        var configPath = Path.Combine(_tempDir, "config.json");
        var config = CliConfiguration.Load(configPath);
        config.SetProfile("default", "https://example.com", "token");
        config.Save();

        var factory = new ApiClientFactory(config);
        var httpClient = factory.CreateHttpClient();

        httpClient.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void UploadToken_IsRecognizedOnlyWithCfuPrefix()
    {
        ApiClientFactory.IsUploadToken("cfu_test").Should().BeTrue();
        ApiClientFactory.IsUploadToken("cf4_test").Should().BeFalse();
    }

    [Fact]
    public void Environment_ProvidesConfigurationWithoutSavedProfile()
    {
        var config = CliConfiguration.Load(Path.Combine(_tempDir, "missing.json"));
        var factory = new ApiClientFactory(config, environment: name => name switch
        {
            "CF_URL" => "http://localhost:1234/",
            "CF_TOKEN" => "cfu_from_environment",
            _ => null,
        });
        var profile = factory.GetProfile();
        profile.Url.Should().Be("http://localhost:1234");
        profile.Token.Should().Be("cfu_from_environment");
        factory.Create().Should().NotBeNull();
    }

    [Fact]
    public void TokenOverride_WorksWithOnlyUrlInEnvironment()
    {
        var config = CliConfiguration.Load(Path.Combine(_tempDir, "missing.json"));
        var factory = new ApiClientFactory(config, environment: name => name == "CF_URL" ? "http://localhost" : null);
        factory.GetProfile(tokenOverride: "cfu_override").Token.Should().Be("cfu_override");
    }
}
