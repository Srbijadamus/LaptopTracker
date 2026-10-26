using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaptopTracker.Models
{
    public enum ReturnDeviceStatus { PendingPickup, PickedUp }

    [Index(nameof(Date))]
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
        public string? KID { get; set; }
        public bool? ChargerReturned { get; set; }
        public bool? PowerCableReturned { get; set; }
        public string? DamageStatus { get; set; }
        public string? DamageComment { get; set; }
        public DateTime? LeaseEndDatePlanned { get; set; }
        public bool IsCustomerInducedDamage { get; set; } = false;
        public bool IsBatterySwollen { get; set; } = false;
        public string? Acknowledgement { get; set; }

        // Same reference data as WicStockDevice.Model: free string matched against
        // Models.ModelName at read time, nullable so "not recorded" stays NULL.
        public string? Model { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public List<ReturnDevicePhoto> Photos { get; set; } = new();
    }
}

