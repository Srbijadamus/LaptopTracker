using System.ComponentModel.DataAnnotations;

namespace LaptopTracker.Models
{
    public class Handover
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Kid { get; set; } = string.Empty;
        [Required] public string Location { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string? Signature { get; set; }
        public List<HandoverDevice> Devices { get; set; } = new();
    }
}
