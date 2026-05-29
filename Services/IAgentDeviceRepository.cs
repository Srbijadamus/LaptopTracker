using LaptopTracker.Models.Agent;

namespace LaptopTracker.Services
{
    public interface IAgentDeviceRepository
    {
        bool IsValidSource(string source);

        Task<List<DeviceDto>>        FindDeviceExactAsync(string identifier);
        Task<List<DeviceDto>>        FindDevicesByUserAsync(string userName);
        Task<List<DeviceDto>>        ListDevicesByStatusAsync(string status, string? source);
        Task<List<DeviceDto>>        ListDevicesByLocationAsync(string location, string? source);
        Task<List<DeviceDto>>        ListNotReturnedAsync(string? source);
        Task<List<DeviceDto>>        ListPendingPickupsAsync(string? source);
        Task<List<InconsistencyDto>> DetectInconsistenciesAsync(string? source);

        Task<DeviceDto?> GetDeviceBySourceAndIdAsync(string source, int id);

        Task<(bool ok, DeviceDto? device, string? error)> UpdateStatusAsync(string source, int id, string status);
        Task<(bool ok, DeviceDto? device, string? error)> UpdateLocationAsync(string source, int id, string deviceLocation);
        Task<(bool ok, DeviceDto? device, string? error)> UpdateAssignmentAsync(string source, int id, string? kidHandedTo, string? kid, DateTime? dateHandedOver);
        Task<(bool ok, DeviceDto? device, string? error)> MarkReturnedAsync(string source, int id);
    }
}
