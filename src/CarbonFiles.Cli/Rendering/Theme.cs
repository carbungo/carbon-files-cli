using CarbonFiles.Cli.Infrastructure;
using Spectre.Console;

namespace CarbonFiles.Cli.Rendering;

public static class Theme
{
    // Emoji constants
    public static string Fire => Emoji.Known.Fire;
    public static string Package => Emoji.Known.Package;
    public static string Skull => Emoji.Known.Skull;
    public static string Rocket => Emoji.Known.Rocket;
    public static string Crab => Emoji.Known.Crab;
    public static string Gear => Emoji.Known.Gear;
    public static string Sparkles => Emoji.Known.Sparkles;
    public static string GreenHeart => Emoji.Known.GreenHeart;
    public static string HighVoltage => Emoji.Known.HighVoltage;
    public static string PartyPopper => Emoji.Known.PartyPopper;
    public static string Collision => Emoji.Known.Collision;
    public static string CheckMark => Emoji.Known.CheckMark;
    public static string CrossMark => Emoji.Known.CrossMark;
    public static string InboxTray => Emoji.Known.InboxTray;
    public static string OutboxTray => Emoji.Known.OutboxTray;
    public static string Globe => Emoji.Known.GlobeWithMeridians;
    public static string LightBulb => Emoji.Known.LightBulb;
    public static string Locked => Emoji.Known.Locked;
    public static string GreenCircle => Emoji.Known.GreenCircle;
    public static string RedCircle => Emoji.Known.RedCircle;
    public static string YellowCircle => Emoji.Known.YellowCircle;
    public static string Cyclone => Emoji.Known.Cyclone;
    public static string Sync => Emoji.Known.ClockwiseVerticalArrows;
    public static string MagnifyingGlass => Emoji.Known.MagnifyingGlassTiltedRight;

    public static Table CreateTable()
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        return table;
    }
}
