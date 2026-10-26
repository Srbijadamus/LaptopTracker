using System.ComponentModel.DataAnnotations;

namespace LaptopTracker.Models
{
    // Reference table backing the WIC Stock "Model" dropdown. Maps to table "Models".
    // SizeClass is a stored value here, not parsed at runtime — ModelSizeClassifier
    // is not used for this classification path.
    public class DeviceModel
    {
        public int Id { get; set; }

        [Required] public string ModelName { get; set; } = string.Empty;
        public SizeClass SizeClass { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
