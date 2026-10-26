using LaptopTracker.Models;

namespace LaptopTracker.Helpers
{
    // Backed by the WicLocations/LocationAliases tables (loaded once at startup via Load()).
    // No hardcoded arrays: this class keeps the pre-existing static API so views/controllers
    // built against LocationList.* keep working unchanged.
    public static class LocationList
    {
        private static readonly Dictionary<string, string> CountryCodes = new()
        {
            ["Germany"] = "DE",
            ["Czech Republic"] = "CZ",
            ["Hungary"] = "HU",
            ["Italy"] = "IT",
            ["Netherlands"] = "NL",
            ["Poland"] = "PL",
            ["Sweden"] = "SE",
            ["United Kingdom"] = "GB",
        };

        private static Dictionary<string, string> _aliasToCanonical = new();

        public static List<string> SiteLocations { get; private set; } = new();

        public static List<string> Locations { get; private set; } = new();

        public static Dictionary<string, string> LocationCountry { get; private set; } = new();

        public static void Load(IEnumerable<WicLocation> locations)
        {
            var list = locations
                .OrderBy(l => l.DeviceLocationKey, StringComparer.Ordinal)
                .ToList();

            SiteLocations = list.Select(l => l.DeviceLocationKey).ToList();

            Locations = SiteLocations
                .Concat(new[] { "In WIC", "In Transit", "In Use" })
                .ToList();

            LocationCountry = list.ToDictionary(
                l => l.DeviceLocationKey,
                l => CountryCodes.TryGetValue(l.Country, out var code) ? code : "");

            _aliasToCanonical = list
                .SelectMany(l => l.Aliases.Select(a => (a.Alias, l.DeviceLocationKey)))
                .ToDictionary(x => x.Alias, x => x.DeviceLocationKey);
        }

        // Alias -> canonical resolution, the only place this lookup happens. Returns null
        // when raw is neither a canonical DeviceLocationKey nor a known alias (legacy/unmatched).
        public static string? Resolve(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (SiteLocations.Contains(raw)) return raw;
            return _aliasToCanonical.TryGetValue(raw, out var canonical) ? canonical : null;
        }

        public static string GetCountry(string location) =>
            LocationCountry.TryGetValue(location, out var c) ? c : "";

        public static List<string>? GetLocationsByCountry(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return null;
            return LocationCountry
                .Where(kv => kv.Value == countryCode)
                .Select(kv => kv.Key)
                .ToList();
        }
    }
}
