using CarbonFiles.Cli.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Infrastructure;

public sealed class VerboseInterceptor(ApiClientFactory factory) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is GlobalSettings { Verbose: true })
        {
            factory.VerboseConsole = AnsiConsole.Console;
        }
    }
}
