using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LaptopTracker.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SizeClass { S, M, L, D }

    public class IdealStockTarget
    {
        public int Id { get; set; }

        [Required] public string Location { get; set; } = string.Empty;
        public SizeClass SizeClass { get; set; }
        public int TargetQuantity { get; set; }

        // Only meaningful when SizeClass == D: which device type this location's "D" row
        // currently represents (Lenovo / Rugged tablet / Microsoft Surface tablet / Apple tablet).
        public string? DeviceTypeLabel { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Required] public string UpdatedBy { get; set; } = "unknown";
    }
}
