using System.ComponentModel.DataAnnotations;

namespace LaptopTracker.Models
{
    public enum ReturnDeviceStatus { PendingPickup, PickedUp }

    public class ReturnDevice
    {
        public int Id { get; set; }

        [Required] public string SerialNumber { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "Lenovo";
        public string DeviceStateType { get; set; } = "Return";
        [Required] public string RITM { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string DeviceLocation { get; set; } = "In WIC";
        public ReturnDeviceStatus Status { get; set; } = ReturnDeviceStatus.PendingPickup;
        public string? WorkOrder { get; set; }
        public string? PickupStatus { get; set; }
        public string? Location { get; set; }
        public bool? ChargerReturned { get; set; }
        public bool? PowerCableReturned { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
