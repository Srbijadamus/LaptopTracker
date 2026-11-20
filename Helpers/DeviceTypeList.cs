namespace LaptopTracker.Helpers
{
    // Single ordered source of truth for the Device Type dropdown. The order here is the
    // order shown in every Create/Edit form and list filter; HP is first, so it is the
    // default on Create forms. List filters append any DB values not listed here (legacy
    // types still present in the data) so nothing disappears from the dropdown.
    public static class DeviceTypeList
    {
        public static readonly string[] Ordered =
        {
            "HP",
            "Lenovo",
            "Dell",
            "Apple iPad",
            "Desktop",
            "Rugged tablet",
            "Microsoft Surface tablet",
            "Apple tablet",
        };

        // Ordered list first, then any values from the data that aren't already listed.
        public static List<string> Merge(IEnumerable<string> existing) =>
            Ordered.Concat(
                existing
                    .Where(v => !string.IsNullOrWhiteSpace(v) && !Ordered.Contains(v))
                    .Distinct()
            ).ToList();
    }
}
