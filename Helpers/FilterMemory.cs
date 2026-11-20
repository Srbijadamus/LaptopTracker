using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace LaptopTracker.Helpers
{
    /// <summary>
    /// Remembers a list's dropdown filters (Status, Location/Site, Type, WIC, Category
    /// and the Returns Active/Collected tab) per user (KID) and per list, so agents don't
    /// have to re-set them every time they return to a list. Values are stored in a
    /// persistent 30-day cookie, keyed by list + KID so "Switch User" gives each user
    /// their own filters. Free-text search and date ranges are deliberately NOT
    /// remembered — they are one-off searches.
    /// </summary>
    public static class FilterMemory
    {
        private const string Prefix = "ListFilters_";
        private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(30);

        public static readonly string[] ReturnDeviceDropdownKeys =
            { "statusFilter", "locationFilter", "deviceTypeFilter" };
        public static readonly string[] WicStockDropdownKeys =
            { "statusFilter", "siteFilter", "deviceTypeFilter", "categoryFilter" };
        public static readonly string[] LoanerDeviceDropdownKeys =
            { "statusFilter", "locationFilter", "deviceTypeFilter", "wicFilter" };

        private static string CookieName(string list, string? kid) => $"{Prefix}{list}_{(kid ?? string.Empty)}";

        private static Dictionary<string, string> GetSaved(HttpContext context, string list, string? kid)
        {
            if (!context.Request.Cookies.TryGetValue(CookieName(list, kid), out var raw) ||
                string.IsNullOrWhiteSpace(raw))
                return new Dictionary<string, string>();

            try
            {
                var json = Uri.UnescapeDataString(raw);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                    ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Resolves the effective dropdown-filter values for a list.
        /// If the URL carries any of the tracked keys, those take priority (an explicit
        /// filter action) and the result is saved. Otherwise the saved values are applied
        /// (e.g. returning from the sidebar, Dashboard, Edit→Cancel or a Save redirect).
        /// </summary>
        public static Dictionary<string, string> ResolveDropdowns(
            IQueryCollection query, HttpContext context, string list, string? kid, string[] keys)
        {
            var saved = GetSaved(context, list, kid);
            var urlHasAny = keys.Any(k => query.ContainsKey(k));

            var result = new Dictionary<string, string>();
            if (urlHasAny)
            {
                foreach (var k in keys)
                    result[k] = query.ContainsKey(k) ? (query[k].ToString() ?? string.Empty) : string.Empty;
                Save(context, list, kid, result);
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
        public static string ResolveTab(IQueryCollection query, HttpContext context, string list, string? kid)
        {
            if (query.TryGetValue("tab", out var fromUrl))
            {
                var tab = fromUrl.ToString();
                if (tab != "active" && tab != "collected") tab = "active";
                SaveTab(context, list, kid, tab);
                return tab;
            }

            var saved = GetSaved(context, list, kid);
            if (saved.TryGetValue("tab", out var savedTab) && savedTab == "collected")
                return "collected";
            return "active";
        }

        private static void SaveTab(HttpContext context, string list, string? kid, string tab)
        {
            var saved = GetSaved(context, list, kid);
            saved["tab"] = tab;
            Save(context, list, kid, saved);
        }

        private static void Save(HttpContext context, string list, string? kid, IDictionary<string, string> values)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = CookieLifetime,
            };
            context.Response.Cookies.Append(
                CookieName(list, kid),
                Uri.EscapeDataString(JsonSerializer.Serialize(values)),
                options);
        }

        /// <summary>Clears all remembered filters (and the tab) for a list + user.</summary>
        public static void Clear(HttpContext context, string list, string? kid)
        {
            context.Response.Cookies.Delete(CookieName(list, kid));
        }
    }
}
