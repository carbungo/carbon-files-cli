using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Commands.Config;
using CarbonFiles.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var services = new ServiceCollection();

var cliConfig = CliConfiguration.Load();
services.AddSingleton(cliConfig);

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("cf");
    config.SetApplicationVersion("0.1.0");

    config.AddBranch("bucket", b =>
    {
        b.SetDescription("Manage buckets.");
        b.AddCommand<BucketListCommand>("list").WithDescription("List all buckets.");
        b.AddCommand<BucketCreateCommand>("create").WithDescription("Create a new bucket.");
        b.AddCommand<BucketInfoCommand>("info").WithDescription("Show bucket details.");
    });

    config.AddBranch("config", b =>
    {
        b.SetDescription("Manage CLI configuration and profiles.");
        b.AddCommand<ConfigSetCommand>("set")
            .WithDescription("Set server URL and authentication token.");
        b.AddCommand<ConfigShowCommand>("show")
            .WithDescription("Show current profile configuration.");
        b.AddCommand<ConfigProfilesCommand>("profiles")
            .WithDescription("List all saved profiles.");
        b.AddCommand<ConfigUseCommand>("use")
            .WithDescription("Switch active profile.");
    });
});

return app.Run(args);
