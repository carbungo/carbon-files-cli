using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;
using Refit;
using Spectre.Console.Testing;
using System.Net;

namespace CarbonFiles.Cli.Tests.Infrastructure;

public class ErrorHandlerTests
{
    [Fact]
    public async Task Handle_ApiException_WithErrorBody_ShowsErrorAndHint()
    {
        var console = new TestConsole();
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"Bucket not found\",\"hint\":\"Check the bucket ID\"}")
        };
        var ex = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "http://test"),
            HttpMethod.Get,
            response,
            new RefitSettings());

        var result = ErrorHandler.Handle(ex, console);

        result.Should().Be(1);
        console.Output.Should().Contain("Bucket not found");
        console.Output.Should().Contain("Check the bucket ID");
    }

    [Fact]
    public async Task Handle_ApiException_WithErrorBodyNoHint_ShowsErrorOnly()
    {
        var console = new TestConsole();
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"Something went wrong\"}")
        };
        var ex = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "http://test"),
            HttpMethod.Get,
            response,
            new RefitSettings());

        var result = ErrorHandler.Handle(ex, console);

        result.Should().Be(1);
        console.Output.Should().Contain("Something went wrong");
        console.Output.Should().NotContain("Hint:");
    }

    [Fact]
    public async Task Handle_ApiException_WithNonJsonBody_ShowsStatusCode()
    {
        var console = new TestConsole();
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("plain text error")
        };
        var ex = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "http://test"),
            HttpMethod.Get,
            response,
            new RefitSettings());

        var result = ErrorHandler.Handle(ex, console);

        result.Should().Be(1);
        console.Output.Should().Contain("500");
    }

    [Fact]
    public async Task Handle_ApiException_Unauthorized_ShowsAuthHint()
    {
        var console = new TestConsole();
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("")
        };
        var ex = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "http://test"),
            HttpMethod.Get,
            response,
            new RefitSettings());

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
