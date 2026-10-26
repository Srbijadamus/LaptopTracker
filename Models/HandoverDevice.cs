namespace LaptopTracker.Models
{
    public class HandoverDevice
    {
        public int Id { get; set; }
        public int HandoverId { get; set; }
        public Handover Handover { get; set; } = null!;
        public int DeviceId { get; set; }
        public string DeviceType { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;

        // Snapshot of the source device's Work Order, taken when the handover is created, so the
        // printed handover still shows it after the source row was soft-deleted or purged.
        // Nullable: existing handover lines keep NULL and are resolved by a live lookup instead.
        public string? WorkOrder { get; set; }
    }
}
