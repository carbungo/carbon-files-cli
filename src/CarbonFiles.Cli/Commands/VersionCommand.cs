using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands;

public sealed class VersionCommand(IAnsiConsole console) : AsyncCommand<GlobalSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellation)
    {
        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(new
            {
                Version = BuildInfo.InformationalVersion,
                Commit = BuildInfo.GitCommitFull,
                BuildDate = BuildInfo.BuildDate,
            }));
            return Task.FromResult(0);
        }

        console.MarkupLine($"[bold blue]{Emoji.Known.Cloud}  CarbonFiles[/] [dim]v{BuildInfo.InformationalVersion}[/]");
        console.WriteLine();

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow("[bold]Version[/]", BuildInfo.InformationalVersion);
        grid.AddRow("[bold]Commit[/]", BuildInfo.GitCommit);
        grid.AddRow("[bold]Built[/]", BuildInfo.BuildDate);

        console.Write(grid);

        return Task.FromResult(0);
    }
}
