using System.ComponentModel.DataAnnotations;

namespace LaptopTracker.Models
{
    public enum LoanerStatus { Available, NotAvailable }

    public class LoanerDevice
    {
        public int Id { get; set; }

        [Required] public string SerialNumber { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "Lenovo";
        public string DeviceStateType { get; set; } = "Loaner Device";
        [Required] public string RITM { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string DeviceLocation { get; set; } = "In WIC";
        public LoanerStatus Status { get; set; } = LoanerStatus.Available;
        public string? KidHandedTo { get; set; }
        public DateTime? DateHandedOver { get; set; }
        public string? WIC { get; set; }
        public string? KID { get; set; }
        public string? UserAddress { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}

