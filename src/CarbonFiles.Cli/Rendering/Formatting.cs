namespace CarbonFiles.Cli.Rendering;

public static class Formatting
{
    private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB"];

    public static string FormatSize(long bytes)
    {
        if (bytes == 0) return "0 B";

        var order = 0;
        var size = (double)bytes;
        while (size >= 1024 && order < SizeUnits.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return order == 0
            ? $"{size:0} {SizeUnits[order]}"
            : $"{size:0.0} {SizeUnits[order]}";
    }

    public static string FormatDate(DateTime? date)
    {
        if (date is null) return "-";

        var diff = DateTime.UtcNow - date.Value;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 30) return $"{(int)diff.TotalDays}d ago";
        return date.Value.ToString("yyyy-MM-dd");
    }

    public static string FormatExpiry(DateTime? expiresAt)
    {
        if (expiresAt is null) return "[green]Never[/]";

        var remaining = expiresAt.Value - DateTime.UtcNow;
        if (remaining.TotalSeconds <= 0) return $"[red]Expired {FormatDate(expiresAt)}[/]";
        if (remaining.TotalMinutes < 60) return $"[yellow]{(int)remaining.TotalMinutes}m[/]";
        if (remaining.TotalHours < 24) return $"[yellow]{(int)remaining.TotalHours}h[/]";
        return $"[green]{(int)remaining.TotalDays}d[/]";
    }
}
