using System.Net;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using FluentAssertions;
using Spectre.Console.Testing;

namespace CarbonFiles.Cli.Tests.Infrastructure;

public class ErrorHandlerTests
{
    [Fact]
    public void Handle_CarbonFilesException_WithErrorAndHint_ShowsBoth()
    {
        var console = new TestConsole();
        var ex = new CarbonFilesException(HttpStatusCode.BadRequest, "Bucket not found", "Check the bucket ID");

        var result = ErrorHandler.Handle(ex, console);

        result.Should().Be(1);
        console.Output.Should().Contain("Bucket not found");
        console.Output.Should().Contain("Check the bucket ID");
    }

    [Fact]
    public void Handle_CarbonFilesException_WithErrorNoHint_ShowsErrorOnly()
    {
        var console = new TestConsole();
        var ex = new CarbonFilesException(HttpStatusCode.BadRequest, "Something went wrong");

        var result = ErrorHandler.Handle(ex, console);

        result.Should().Be(1);
        console.Output.Should().Contain("Something went wrong");
        console.Output.Should().NotContain("Hint:");
    }

    [Fact]
    public void Handle_CarbonFilesException_Unauthorized_ShowsAuthHint()
    {
        var console = new TestConsole();
        var ex = new CarbonFilesException(HttpStatusCode.Unauthorized, "Unauthorized");

        var result = ErrorHandler.Handle(ex, console);

        result.Should().Be(1);
        console.Output.Should().Contain("cf config show");
    }

    [Fact]
    public void Handle_HttpRequestException_ShowsConnectionError()
    {
        var console = new TestConsole();
        var ex = new HttpRequestException("Connection refused");

        var result = ErrorHandler.Handle(ex, console);

        result.Should().Be(1);
        console.Output.Should().Contain("Connection error");
        console.Output.Should().Contain("cf config show");
    }

    [Fact]
    public void Handle_InvalidOperationException_ConfigError_ShowsMessage()
    {
        var console = new TestConsole();
        var ex = new InvalidOperationException("No server configured. Run: cf config set --url <url> --token <token>");

        var result = ErrorHandler.Handle(ex, console);

        result.Should().Be(1);
        console.Output.Should().Contain("cf config set");
    }

    [Fact]
    public void Handle_GenericException_ShowsErrorMessage()
    {
        var console = new TestConsole();
        var ex = new Exception("Something unexpected happened");

        var result = ErrorHandler.Handle(ex, console);

        result.Should().Be(1);
        console.Output.Should().Contain("Something unexpected happened");
    }
}
