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

        // Real WIC site, separate from DeviceLocation's state values ("In WIC"/"In Transit").
        // Nullable: most existing rows have no site recorded and must stay NULL, never guessed.
        public string? Location { get; set; }

        // Free string matched against Models.ModelName at read time (not an FK) so a
        // value with no match falls into the UNMAPPED bucket instead of failing a constraint.
        public string? Model { get; set; }

        public WicStockStatus Status { get; set; } = WicStockStatus.Available;
        public string? SwapRITM { get; set; }
        public string? SwapSerialNumber { get; set; }
        public DateTime? SwapDate { get; set; }
        public string? SwapStatus { get; set; }

        // Category separate from DeviceLocation (state) and Status, so existing filters and
        // data stay untouched. NULL = no category; "Re-imaged laptops" is the first value.
        public string? Category { get; set; }

        // ── Fields mirrored from ReturnDevice so a returned device keeps its information
        // after it moves into WIC stock. Same types as on ReturnDevice so values copy as
        // stored; all nullable (or false-defaulted) so existing WIC rows stay valid.
        public string? WorkOrder { get; set; }
        public string? PickupStatus { get; set; }
        public string? KID { get; set; }
        public bool? ChargerReturned { get; set; }
        public bool? PowerCableReturned { get; set; }
        public string? DamageStatus { get; set; }
        public string? DamageComment { get; set; }
        public DateTime? LeaseEndDatePlanned { get; set; }
        public bool IsCustomerInducedDamage { get; set; } = false;
        public bool IsBatterySwollen { get; set; } = false;
        public string? Acknowledgement { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public List<WicStockDevicePhoto> Photos { get; set; } = new();
    }
}
