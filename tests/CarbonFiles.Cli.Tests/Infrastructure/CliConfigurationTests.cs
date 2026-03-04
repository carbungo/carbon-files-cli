using System.Text.Json;
using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Infrastructure;

public class CliConfigurationTests : IDisposable
{
    private readonly string _tempDir;

    public CliConfigurationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cf_test_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var config = CliConfiguration.Load(Path.Combine(_tempDir, "config.json"));

        config.Should().NotBeNull();
        config.ActiveProfile.Should().Be("default");
        config.Profiles.Should().BeEmpty();
    }

    [Fact]
    public void Save_CreatesDirectoryAndFile()
    {
        var configPath = Path.Combine(_tempDir, "subdir", "config.json");
        var config = CliConfiguration.Load(configPath);
        config.SetProfile("default", "https://example.com", "cf4_test_token");

        config.Save();

        File.Exists(configPath).Should().BeTrue();
    }

    [Fact]
    public void Save_And_Load_RoundTrips()
    {
        var configPath = Path.Combine(_tempDir, "config.json");
        var config = CliConfiguration.Load(configPath);
        config.SetProfile("prod", "https://prod.example.com", "cf4_prod_token");
        config.SetProfile("local", "http://localhost:5000", "admin-key");
        config.ActiveProfile = "prod";
        config.Save();

        var loaded = CliConfiguration.Load(configPath);

        loaded.ActiveProfile.Should().Be("prod");
        loaded.Profiles.Should().HaveCount(2);
        loaded.GetActiveProfile()!.Url.Should().Be("https://prod.example.com");
        loaded.GetActiveProfile()!.Token.Should().Be("cf4_prod_token");
    }

    [Fact]
    public void GetActiveProfile_NoProfiles_ReturnsNull()
    {
        var config = CliConfiguration.Load(Path.Combine(_tempDir, "config.json"));

        config.GetActiveProfile().Should().BeNull();
    }

    [Fact]
    public void GetActiveProfile_InvalidName_ReturnsNull()
    {
        var configPath = Path.Combine(_tempDir, "config.json");
        var config = CliConfiguration.Load(configPath);
        config.SetProfile("default", "https://example.com", "token");
        config.ActiveProfile = "nonexistent";

        config.GetActiveProfile().Should().BeNull();
    }

    [Fact]
    public void GetMaskedToken_MasksApiKey()
    {
        var profile = new Profile { Url = "https://example.com", Token = "cf4_abcd1234_secretsecretsecretsecret" };

        profile.MaskedToken.Should().Be("cf4_abcd1234_****");
    }

    [Fact]
    public void GetMaskedToken_MasksShortToken()
    {
        var profile = new Profile { Url = "https://example.com", Token = "short" };

        profile.MaskedToken.Should().Be("****");
    }
}
