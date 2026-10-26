using System.ComponentModel.DataAnnotations;

namespace LaptopTracker.Models
{
    // Canonical WIC site master list, seeded from the WIC Shift Plan export.
    public class WicLocation
    {
        public int Id { get; set; }

        [Required] public string Llid { get; set; } = string.Empty;
        [Required] public string Country { get; set; } = string.Empty;
        [Required] public string CityLabel { get; set; } = string.Empty;
        [Required] public string Street { get; set; } = string.Empty;

        // "City - Street" — the canonical string matched against DeviceLocation/Location/WIC values.
        [Required] public string DeviceLocationKey { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<LocationAlias> Aliases { get; set; } = new();
    }

    // Alternate spellings that resolve to a canonical WicLocation (export spelling drift,
    // abbreviated live-data forms, etc.). The only alias mechanism in the codebase.
    public class LocationAlias
    {
        public int Id { get; set; }

        [Required] public string Alias { get; set; } = string.Empty;

        public int LocationId { get; set; }
        public WicLocation Location { get; set; } = null!;
    }
}
