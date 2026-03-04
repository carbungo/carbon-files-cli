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
}
