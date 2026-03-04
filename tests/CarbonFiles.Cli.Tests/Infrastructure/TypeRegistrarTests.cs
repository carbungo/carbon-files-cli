using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonFiles.Cli.Tests.Infrastructure;

public class TypeRegistrarTests
{
    [Fact]
    public void Build_ReturnsTypeResolver()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        var resolver = registrar.Build();

        resolver.Should().NotBeNull();
        resolver.Should().BeOfType<TypeResolver>();
    }

    [Fact]
    public void Register_And_Resolve_ReturnsInstance()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        registrar.Register(typeof(ITestService), typeof(TestService));

        var resolver = registrar.Build();
        var result = resolver.Resolve(typeof(ITestService));

        result.Should().NotBeNull();
        result.Should().BeOfType<TestService>();
    }

    [Fact]
    public void RegisterInstance_And_Resolve_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var instance = new TestService();
        registrar.RegisterInstance(typeof(ITestService), instance);

        var resolver = registrar.Build();
        var result = resolver.Resolve(typeof(ITestService));

        result.Should().BeSameAs(instance);
    }

    [Fact]
    public void Resolve_NullType_ReturnsNull()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var resolver = registrar.Build();

        var result = resolver.Resolve(null);

        result.Should().BeNull();
    }

    private interface ITestService;
    private class TestService : ITestService;
}
