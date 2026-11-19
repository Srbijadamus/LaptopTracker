using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace LaptopTracker.Helpers
{
    /// <summary>
    /// Remembers a list's dropdown filters (Status, Location/Site, Type, WIC, Category
    /// and the Returns Active/Collected tab) per user (KID) and per list, so agents don't
    /// have to re-set them every time they return to a list. Values are stored server-side
    /// in the session (the same place the KID and country filter already live), keyed by
    /// list + KID so "Switch User" gives each user their own filters. Free-text search and
    /// date ranges are deliberately NOT remembered — they are one-off searches.
    /// </summary>
    public static class FilterMemory
    {
        private const string Prefix = "ListFilters_";

        public static readonly string[] ReturnDeviceDropdownKeys =
            { "statusFilter", "locationFilter", "deviceTypeFilter" };
        public static readonly string[] WicStockDropdownKeys =
            { "statusFilter", "siteFilter", "deviceTypeFilter", "categoryFilter" };
        public static readonly string[] LoanerDeviceDropdownKeys =
            { "statusFilter", "locationFilter", "deviceTypeFilter", "wicFilter" };

        private static string Key(string list, string? kid) => $"{Prefix}{list}_{(kid ?? string.Empty)}";

        private static Dictionary<string, string> GetSaved(ISession session, string list, string? kid)
        {
            var json = session.GetString(Key(list, kid));
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
            try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(); }
            catch { return new Dictionary<string, string>(); }
        }

        /// <summary>
        /// Resolves the effective dropdown-filter values for a list.
        /// If the URL carries any of the tracked keys, those take priority (an explicit
        /// filter action) and the result is saved. Otherwise the saved values are applied
        /// (e.g. returning from the sidebar, Dashboard, Edit→Cancel or a Save redirect).
        /// </summary>
        public static Dictionary<string, string> ResolveDropdowns(
            IQueryCollection query, ISession session, string list, string? kid, string[] keys)
        {
            var saved = GetSaved(session, list, kid);
            var urlHasAny = keys.Any(k => query.ContainsKey(k));

            var result = new Dictionary<string, string>();
            if (urlHasAny)
            {
                foreach (var k in keys)
                    result[k] = query.ContainsKey(k) ? (query[k].ToString() ?? string.Empty) : string.Empty;
                Save(session, list, kid, result);
            }
            else
            {
                foreach (var k in keys)
                    result[k] = saved.TryGetValue(k, out var v) ? v : string.Empty;
            }
            return result;
        }

        /// <summary>
        /// Resolves the Returns Active/Collected tab. A "tab" value in the URL wins and is
        /// saved; otherwise the saved tab (default "active") is used. Tab is handled
        /// separately because it is outside the filter form and never submitted with it.
        /// </summary>
        public static string ResolveTab(IQueryCollection query, ISession session, string list, string? kid)
        {
            if (query.TryGetValue("tab", out var fromUrl))
            {
                var tab = fromUrl.ToString();
                if (tab != "active" && tab != "collected") tab = "active";
                SaveTab(session, list, kid, tab);
                return tab;
            }

            var saved = GetSaved(session, list, kid);
            if (saved.TryGetValue("tab", out var savedTab) && savedTab == "collected")
                return "collected";
            return "active";
        }

        private static void SaveTab(ISession session, string list, string? kid, string tab)
        {
            var saved = GetSaved(session, list, kid);
            saved["tab"] = tab;
            Save(session, list, kid, saved);
        }

        private static void Save(ISession session, string list, string? kid, IDictionary<string, string> values)
        {
            session.SetString(Key(list, kid), JsonSerializer.Serialize(values));
        }

        /// <summary>Clears all remembered filters (and the tab) for a list + user.</summary>
        public static void Clear(ISession session, string list, string? kid)
        {
            session.Remove(Key(list, kid));
        }
    }
}
