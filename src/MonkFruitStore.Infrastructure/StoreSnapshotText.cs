using System.Text.RegularExpressions;

namespace MonkFruitStore.Infrastructure;

internal static class StoreSnapshotText
{
    private static readonly Regex LegacyProductPhrase = new(
        @"monk\s+fruit\s+sweetener",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string Clean(string json)
    {
        return LegacyProductPhrase.Replace(json, "sweetener");
    }
}
