namespace LaptopTracker.Models
{
    public class AuditLog
    {
        public long Id { get; set; }
        public string Kid { get; set; } = "";
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string EntityId { get; set; } = "";
        public string? EntityLabel { get; set; }
        public string? Changes { get; set; }
        public string? IpAddress { get; set; }
        public string? Route { get; set; }
        public string Source { get; set; } = "Web";
        public string? RequestId { get; set; }
    }
}
