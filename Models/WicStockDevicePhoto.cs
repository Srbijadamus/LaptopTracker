namespace LaptopTracker.Models
{
    // WIC-stock counterpart of ReturnDevicePhoto. A separate entity because
    // ReturnDevicePhoto.ReturnDeviceId is a required FK and its query filter reads
    // ReturnDevice.IsDeleted, so it cannot carry WIC-stock rows without altering
    // existing columns.
    public class WicStockDevicePhoto
    {
        public int Id { get; set; }
        public int WicStockDeviceId { get; set; }
        public WicStockDevice WicStockDevice { get; set; } = null!;
        public string FilePath { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsSignature { get; set; } = false;
    }
}
