namespace LaptopTracker.Models
{
    public enum StockStatus { Ok, Surplus, Shortage, Undefined }

    public class SizeClassStockRow
    {
        public SizeClass SizeClass { get; set; }
        public int InStock { get; set; }
        public int Reserved { get; set; }
        public int? IdealTarget { get; set; }
        public int? Difference { get; set; }
        public StockStatus Status { get; set; }

        // Only populated for SizeClass == D: which device type this location's "D" row
        // currently represents (Lenovo / Rugged tablet / Microsoft Surface tablet / Apple tablet).
        public string? DeviceTypeLabel { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    // Devices at a location whose Model is NULL or does not match any row in the
    // Models table. Never folded into a SizeClassStockRow, never hidden.
    public class UnclassifiedBucket
    {
        public int InStock { get; set; }
        public int Reserved { get; set; }
        public int Total { get; set; }
        public List<string> Models { get; set; } = new();
        public bool Any => Total > 0;
    }

    public class LocationStockGroup
    {
        public string Location { get; set; } = string.Empty;

        // True for the single synthetic "Unassigned (legacy)" block (devices with NULL
        // WicStockDevice.Location) — never a real site, never editable via SetTarget/ApplyToAll.
        public bool IsUnassigned { get; set; }

        // True when Location is a non-null value that isn't one of the seeded WicLocations
        // (should not happen once the whitelist is enforced everywhere, but the block still
        // renders rather than silently dropping the devices).
        public bool IsUnseededLegacy { get; set; }

        public int DeviceCount { get; set; }
        public List<SizeClassStockRow> Rows { get; set; } = new();
        public UnclassifiedBucket Unclassified { get; set; } = new();
    }

    public class IdealStockOverviewViewModel
    {
        public List<LocationStockGroup> Locations { get; set; } = new();
        public int TotalDeviceCount { get; set; }
    }
}
