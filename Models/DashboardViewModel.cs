namespace LaptopTracker.Models
{
    public class DashboardViewModel
    {
        public int ReturnCount { get; set; }
        public int WicStockCount { get; set; }
        public int LoanerCount { get; set; }
        public int PendingCount { get; set; }

        public List<StatusSlice> StatusDistribution { get; set; } = new();
        public List<KindCount> DevicesByKind { get; set; } = new();
        public List<RecentDevice> RecentDevices { get; set; } = new();
        public List<RecentAudit> RecentAudits { get; set; } = new();
        public bool AuditAvailable { get; set; }
    }

    public record StatusSlice(string Label, int Count, string Color);

    public record KindCount(string Kind, int Count);

    public record RecentDevice(
        int Id,
        string SerialNumber,
        string DeviceType,
        string Kind,
        string RITM,
        string StatusLabel,
        string StatusColor,
        DateTime Date);

    public record RecentAudit(
        string Kid,
        string Action,
        string? EntityLabel,
        DateTime TimestampUtc);
}
