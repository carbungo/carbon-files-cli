using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarbonFiles.Cli.Tests.Infrastructure;

/// <summary>
/// A mock HTTP handler for testing that returns pre-configured responses based on request URL/method.
/// </summary>
public class MockHttpHandler : HttpMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly List<(Func<HttpRequestMessage, bool> Predicate, Func<HttpRequestMessage, HttpResponseMessage> Response)> _handlers = [];
    private readonly List<HttpRequestMessage> _requests = [];

    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    public void Setup(HttpMethod method, string urlPattern, object responseBody, HttpStatusCode status = HttpStatusCode.OK)
    {
        _handlers.Add((
            req => req.Method == method && req.RequestUri?.PathAndQuery.Contains(urlPattern) == true,
            _ => new HttpResponseMessage(status)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(responseBody, JsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json")
            }
        ));
    }

    public void SetupText(HttpMethod method, string urlPattern, string responseBody, HttpStatusCode status = HttpStatusCode.OK)
    {
        _handlers.Add((
            req => req.Method == method && req.RequestUri?.PathAndQuery.Contains(urlPattern) == true,
            _ => new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "text/plain")
            }
        ));
    }

    public void SetupDelete(string urlPattern)
    {
        _handlers.Add((
            req => req.Method == HttpMethod.Delete && req.RequestUri?.PathAndQuery.Contains(urlPattern) == true,
            _ => new HttpResponseMessage(HttpStatusCode.NoContent)
        ));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        foreach (var (predicate, response) in _handlers)
        {
            if (predicate(request))
            {
                return Task.FromResult(response(request));
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\":\"Not found\"}", System.Text.Encoding.UTF8, "application/json")
        });
    }
}
