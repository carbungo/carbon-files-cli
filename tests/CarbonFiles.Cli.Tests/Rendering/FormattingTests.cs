using CarbonFiles.Cli.Rendering;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Rendering;

public class FormattingTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.0 GB")]
    [InlineData(1099511627776, "1.0 TB")]
    public void FormatSize_FormatsCorrectly(long bytes, string expected)
    {
        Formatting.FormatSize(bytes).Should().Be(expected);
    }

    [Fact]
    public void FormatDate_RecentDate_ShowsRelative()
    {
        var recent = DateTime.UtcNow.AddMinutes(-5);
        Formatting.FormatDate(recent).Should().Contain("ago");
    }

    [Fact]
    public void FormatDate_Null_ShowsDash()
    {
        Formatting.FormatDate(null).Should().Be("-");
    }

    [Fact]
    public void FormatExpiry_Null_ShowsNever()
    {
        Formatting.FormatExpiry(null).Should().Be("[green]Never[/]");
    }

    [Fact]
    public void FormatExpiry_Expired_ShowsRedExpired()
    {
        var expired = DateTime.UtcNow.AddHours(-1);
        Formatting.FormatExpiry(expired).Should().Contain("[red]");
    }

    [Fact]
    public void FormatExpiry_Future_ShowsTimeRemaining()
    {
        var future = DateTime.UtcNow.AddDays(5);
        var result = Formatting.FormatExpiry(future);
        result.Should().Contain("d");
    }
}
