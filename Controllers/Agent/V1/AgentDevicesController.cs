using LaptopTracker.Models.Agent;
using LaptopTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaptopTracker.Controllers.Agent.V1
{
    [ApiController]
    [Route("api/agent/v1")]
    public class AgentDevicesController : ControllerBase
    {
        private readonly IAgentDeviceRepository _repo;

        public AgentDevicesController(IAgentDeviceRepository repo)
        {
            _repo = repo;
        }

        // GET /api/agent/v1/devices/lookup?identifier=...
        [HttpGet("devices/lookup")]
        public async Task<IActionResult> FindDeviceExact([FromQuery] string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return Ok(ApiResult<List<DeviceDto>>.Fail("INVALID_INPUT", "identifier is required."));

            var devices = await _repo.FindDeviceExactAsync(identifier);
            return Ok(ApiResult<List<DeviceDto>>.Success(devices));
        }

        // GET /api/agent/v1/users/{userName}/devices
        [HttpGet("users/{userName}/devices")]
        public async Task<IActionResult> FindDevicesByUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Ok(ApiResult<List<DeviceDto>>.Fail("INVALID_INPUT", "userName is required."));

            var devices = await _repo.FindDevicesByUserAsync(userName);
            return Ok(ApiResult<List<DeviceDto>>.Success(devices));
        }

        // GET /api/agent/v1/devices?status=...&source=...
        // GET /api/agent/v1/devices?location=...&source=...
        [HttpGet("devices")]
        public async Task<IActionResult> ListDevices(
            [FromQuery] string? status,
            [FromQuery] string? location,
            [FromQuery] string? source)
        {
            if (!string.IsNullOrWhiteSpace(source) &&
                !source.Equals("all", StringComparison.OrdinalIgnoreCase) &&
                !_repo.IsValidSource(source))
            {
                return Ok(ApiResult<List<DeviceDto>>.Fail("INVALID_SOURCE",
                    $"Invalid source '{source}'. Valid values: ReturnDevices, LoanerDevices, WicStock, all."));
            }

            if (string.IsNullOrWhiteSpace(status) && string.IsNullOrWhiteSpace(location))
                return Ok(ApiResult<List<DeviceDto>>.Fail("INVALID_INPUT",
                    "At least one of 'status' or 'location' query parameters is required."));

            var result = new List<DeviceDto>();

            if (!string.IsNullOrWhiteSpace(status))
            {
                var byStatus = await _repo.ListDevicesByStatusAsync(status, source);
                result.AddRange(byStatus);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var byLocation = await _repo.ListDevicesByLocationAsync(location, source);
                foreach (var d in byLocation)
                {
                    if (!result.Any(x => x.Source == d.Source && x.Id == d.Id))
                        result.Add(d);
                }
            }

            return Ok(ApiResult<List<DeviceDto>>.Success(result));
        }

        // GET /api/agent/v1/devices/missing-scans?source=...
        [HttpGet("devices/missing-scans")]
        public IActionResult ListMissingScans([FromQuery] string? source)
        {
            return Ok(ApiResult<List<DeviceDto>>.Fail("NOT_SUPPORTED",
                "list_missing_scans is not supported by the current schema because no scan timestamp exists."));
        }

        // GET /api/agent/v1/devices/not-returned?source=...
        [HttpGet("devices/not-returned")]
        public async Task<IActionResult> ListNotReturned([FromQuery] string? source)
        {
            if (!string.IsNullOrWhiteSpace(source) &&
                !source.Equals("all", StringComparison.OrdinalIgnoreCase) &&
                !_repo.IsValidSource(source))
            {
                return Ok(ApiResult<List<DeviceDto>>.Fail("INVALID_SOURCE",
                    $"Invalid source '{source}'. Valid values: ReturnDevices, LoanerDevices, WicStock, all."));
            }

            var devices = await _repo.ListNotReturnedAsync(source);
            return Ok(ApiResult<List<DeviceDto>>.Success(devices));
        }

        // GET /api/agent/v1/devices/pending-pickups?source=...
        [HttpGet("devices/pending-pickups")]
        public async Task<IActionResult> ListPendingPickups([FromQuery] string? source)
        {
            if (!string.IsNullOrWhiteSpace(source) &&
                !source.Equals("all", StringComparison.OrdinalIgnoreCase) &&
                !_repo.IsValidSource(source))
            {
                return Ok(ApiResult<List<DeviceDto>>.Fail("INVALID_SOURCE",
                    $"Invalid source '{source}'. Valid values: ReturnDevices, LoanerDevices, WicStock, all."));
            }

            var devices = await _repo.ListPendingPickupsAsync(source);
            return Ok(ApiResult<List<DeviceDto>>.Success(devices));
        }

        // GET /api/agent/v1/inconsistencies?source=...
        [HttpGet("inconsistencies")]
        public async Task<IActionResult> DetectInconsistencies([FromQuery] string? source)
        {
            if (!string.IsNullOrWhiteSpace(source) &&
                !source.Equals("all", StringComparison.OrdinalIgnoreCase) &&
                !_repo.IsValidSource(source))
            {
                return Ok(ApiResult<List<InconsistencyDto>>.Fail("INVALID_SOURCE",
                    $"Invalid source '{source}'. Valid values: ReturnDevices, LoanerDevices, WicStock, all."));
            }

            var items = await _repo.DetectInconsistenciesAsync(source);
            return Ok(ApiResult<List<InconsistencyDto>>.Success(items));
        }
    }
}
