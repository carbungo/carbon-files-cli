using CarbonFiles.Cli.Commands.Bucket;
using CarbonFiles.Cli.Commands.Config;
using CarbonFiles.Cli.Commands.Files;
using CarbonFiles.Cli.Commands.Key;
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
        b.AddCommand<BucketUpdateCommand>("update").WithDescription("Update a bucket.");
        b.AddCommand<BucketDeleteCommand>("delete").WithDescription("Delete a bucket.");
        b.AddCommand<BucketDownloadCommand>("download").WithDescription("Download bucket as ZIP.");
    });

    config.AddBranch("file", b =>
    {
        b.SetDescription("Manage files in buckets.");
        b.AddCommand<FileListCommand>("list").WithDescription("List files in a bucket.");
        b.AddCommand<FileInfoCommand>("info").WithDescription("Show file details.");
        b.AddCommand<FileDeleteCommand>("delete").WithDescription("Delete a file.");
        b.AddCommand<FileUploadCommand>("upload").WithDescription("Upload files to a bucket.");
        b.AddCommand<FileDownloadCommand>("download").WithDescription("Download a file from a bucket.");
    });

    config.AddBranch("key", b =>
    {
        b.SetDescription("Manage API keys.");
        b.AddCommand<KeyListCommand>("list").WithDescription("List all API keys.");
        b.AddCommand<KeyCreateCommand>("create").WithDescription("Create a new API key.");
        b.AddCommand<KeyDeleteCommand>("delete").WithDescription("Revoke an API key.");
        b.AddCommand<KeyUsageCommand>("usage").WithDescription("Show API key usage stats.");
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
