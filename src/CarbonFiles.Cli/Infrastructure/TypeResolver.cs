using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Infrastructure;

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
{
    public object? Resolve(Type? type)
        => type is null ? null : provider.GetService(type);
}
