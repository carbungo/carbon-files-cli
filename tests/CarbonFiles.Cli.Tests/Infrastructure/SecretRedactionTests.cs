using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Infrastructure;

public class SecretRedactionTests
{
    [Fact]
    public void Redact_HidesUploadTokensAndTokenQueries()
    {
        SecretRedaction.Redact("request failed at https://example.com/api?token=cfu_secret&x=1")
            .Should().Be("request failed at https://example.com/api?token=[redacted]&x=1");
        SecretRedaction.Redact("cfu_secret").Should().Be("[redacted]");
    }
}
