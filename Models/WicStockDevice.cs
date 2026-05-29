using System.ComponentModel.DataAnnotations;

namespace LaptopTracker.Models
{
    public enum WicStockStatus { Available, NotAvailable }

    public class WicStockDevice
    {
        public int Id { get; set; }

        [Required] public string SerialNumber { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "Lenovo";
        public string DeviceStateType { get; set; } = "WIC Stock";
        [Required] public string RITM { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string DeviceLocation { get; set; } = "In WIC";
        public WicStockStatus Status { get; set; } = WicStockStatus.Available;
        public string? SwapRITM { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
