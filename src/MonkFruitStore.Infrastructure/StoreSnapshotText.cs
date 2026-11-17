using System.Text.RegularExpressions;

namespace MonkFruitStore.Infrastructure;

internal static class StoreSnapshotText
{
    private static readonly Regex LegacyProductPhrase = new(
        @"monk\s+fruit\s+sweetener",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex LegacyClassicDescription = new(
        @"Everyday\s+sweetener\s+for\s+coffee,\s*tea\s+and\s+desserts\.?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string Clean(string json)
    {
        var cleaned = LegacyProductPhrase.Replace(json, "sweetener");
        return LegacyClassicDescription.Replace(cleaned, "MonkEryth");
    }
}
