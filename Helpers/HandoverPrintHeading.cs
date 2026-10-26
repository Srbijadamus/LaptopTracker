namespace LaptopTracker.Helpers
{
    /// <summary>
    /// Heading of the printed handover document. The change request: on paper the Work Order
    /// number is the large heading, not the handover number. Built from the distinct, non-empty
    /// Work Order values of the handover's devices (snapshot plus legacy fallback) in order of
    /// first occurrence: one to three are listed in full, more are shortened with "+N".
    /// No value at all returns null, so the view keeps the "Übergabe #N" heading.
    /// </summary>
    public static class HandoverPrintHeading
    {
        public const int MaxListed = 3;

        public static string? WorkOrders(IEnumerable<string?> workOrders)
        {
            var distinct = workOrders
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => w!)
                .Distinct()
                .ToList();

            if (distinct.Count == 0) return null;
            if (distinct.Count <= MaxListed) return string.Join(", ", distinct);
            return string.Join(", ", distinct.Take(MaxListed)) + "+" + (distinct.Count - MaxListed);
        }
    }
}