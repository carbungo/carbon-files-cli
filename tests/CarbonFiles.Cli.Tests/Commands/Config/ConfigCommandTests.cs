using CarbonFiles.Cli.Commands.Config;
using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace CarbonFiles.Cli.Tests.Commands.Config;

public class ConfigCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public ConfigCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cf_test_{Guid.NewGuid():N}");
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private CommandAppTester CreateApp(CliConfiguration? config = null)
    {
        config ??= CliConfiguration.Load(_configPath);
        var services = new ServiceCollection();
        services.AddSingleton(config);
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.Configure(c =>
        {
            c.AddBranch("config", b =>
            {
                b.AddCommand<ConfigSetCommand>("set");
                b.AddCommand<ConfigShowCommand>("show");
                b.AddCommand<ConfigProfilesCommand>("profiles");
                b.AddCommand<ConfigUseCommand>("use");
            });
        });
        return app;
    }

    [Fact]
    public void ConfigSet_WithUrlAndToken_SavesProfile()
    {
        var app = CreateApp();

        var result = app.Run("config", "set", "--url", "https://example.com", "--token", "cf4_test_secret");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("https://example.com");

        var loaded = CliConfiguration.Load(_configPath);
        loaded.Profiles.Should().ContainKey("default");
        loaded.Profiles["default"].Url.Should().Be("https://example.com");
        loaded.Profiles["default"].Token.Should().Be("cf4_test_secret");
    }

    [Fact]
    public void ConfigShow_WithProfile_DisplaysMaskedToken()
    {
        var config = CliConfiguration.Load(_configPath);
        config.SetProfile("default", "https://example.com", "cf4_test_secret");
        config.Save();
        var app = CreateApp(config);

        var result = app.Run("config", "show");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("cf4_test_****");
        result.Output.Should().NotContain("cf4_test_secret");
    }

    [Fact]
    public void ConfigShow_NoConfig_ShowsSetupHint()
    {
        var app = CreateApp();

        var result = app.Run("config", "show");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("cf config set");
    }

    [Fact]
    public void ConfigProfiles_ListsAllProfiles()
    {
        var config = CliConfiguration.Load(_configPath);
        config.SetProfile("prod", "https://prod.example.com", "cf4_prod_token");
        config.SetProfile("staging", "https://staging.example.com", "cf4_staging_token");
        config.ActiveProfile = "prod";
        config.Save();
        var app = CreateApp(config);

        var result = app.Run("config", "profiles");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("prod");
        result.Output.Should().Contain("staging");
    }

    [Fact]
    public void ConfigProfiles_Empty_ShowsMessage()
    {
        var app = CreateApp();

        var result = app.Run("config", "profiles");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("No profiles configured.");
    }

    [Fact]
    public void ConfigUse_ValidProfile_SwitchesActive()
    {
        var config = CliConfiguration.Load(_configPath);
        config.SetProfile("prod", "https://prod.example.com", "cf4_prod_token");
        config.SetProfile("staging", "https://staging.example.com", "cf4_staging_token");
        config.ActiveProfile = "prod";
        config.Save();
        var app = CreateApp(config);

        var result = app.Run("config", "use", "staging");

        result.ExitCode.Should().Be(0);

        var loaded = CliConfiguration.Load(_configPath);
        loaded.ActiveProfile.Should().Be("staging");
    }

    [Fact]
    public void ConfigUse_InvalidProfile_ShowsError()
    {
        var config = CliConfiguration.Load(_configPath);
        config.SetProfile("default", "https://example.com", "cf4_test_token");
        config.Save();
        var app = CreateApp(config);

        var result = app.Run("config", "use", "nonexistent");

        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("nonexistent");
    }
}
