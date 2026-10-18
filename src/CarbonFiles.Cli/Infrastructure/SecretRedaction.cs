using System.Text.RegularExpressions;

namespace CarbonFiles.Cli.Infrastructure;

internal static partial class SecretRedaction
{
    [GeneratedRegex(@"cfu_[A-Za-z0-9_\-]+", RegexOptions.IgnoreCase)]
    private static partial Regex UploadTokenPattern();

    [GeneratedRegex(@"([?&]token=)[^&#\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex QueryTokenPattern();

    public static string Redact(string value) => UploadTokenPattern().Replace(
        QueryTokenPattern().Replace(value, "$1[redacted]"), "[redacted]");
}
