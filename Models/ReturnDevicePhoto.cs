namespace LaptopTracker.Models
{
    public class ReturnDevicePhoto
    {
        public int Id { get; set; }
        public int ReturnDeviceId { get; set; }
        public ReturnDevice ReturnDevice { get; set; } = null!;
        public string FilePath { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsSignature { get; set; } = false;
    }
}
