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
    }
}
