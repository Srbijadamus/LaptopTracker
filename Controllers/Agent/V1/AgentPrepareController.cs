using LaptopTracker.Models;
using LaptopTracker.Models.Agent;
using LaptopTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaptopTracker.Controllers.Agent.V1
{
    [ApiController]
    [Route("api/agent/v1/actions")]
    public class AgentPrepareController : ControllerBase
    {
        private readonly IAgentDeviceRepository _repo;

        public AgentPrepareController(IAgentDeviceRepository repo)
        {
            _repo = repo;
        }

        // POST /api/agent/v1/actions/suggest
        [HttpPost("suggest")]
        public async Task<IActionResult> SuggestUpdate([FromBody] SuggestUpdateRequest request)
        {
            if (!_repo.IsValidSource(request.Source))
                return Ok(ApiResult<PrepareResult>.Fail("INVALID_SOURCE",
                    $"Invalid source '{request.Source}'. Valid: ReturnDevices, LoanerDevices, WicStock."));

            var device = await _repo.GetDeviceBySourceAndIdAsync(request.Source, request.Id);
            if (device == null)
                return Ok(ApiResult<PrepareResult>.Fail("NOT_FOUND",
                    $"Device not found: source={request.Source}, id={request.Id}."));

            var allowed  = AllowedFields(request.Source);
            var proposed = new Dictionary<string, string?>();
            var warnings = new List<string>();

            foreach (var (field, value) in request.Fields)
            {
                if (allowed.Contains(field))
                    proposed[field] = value;
                else
                    warnings.Add($"Field '{field}' is not writable for source '{request.Source}'.");
            }

            var result = new PrepareResult
            {
                Action          = "suggest",
                CurrentState    = device,
                ProposedChanges = proposed,
                Warning         = warnings.Count > 0 ? string.Join(" ", warnings) : null
            };

            return Ok(ApiResult<PrepareResult>.Success(result));
        }

        // POST /api/agent/v1/actions/prepare/status-update
        [HttpPost("prepare/status-update")]
        public async Task<IActionResult> PrepareStatusUpdate([FromBody] PrepareStatusUpdateRequest request)
        {
            if (!_repo.IsValidSource(request.Source))
                return Ok(ApiResult<PrepareResult>.Fail("INVALID_SOURCE",
                    $"Invalid source '{request.Source}'. Valid: ReturnDevices, LoanerDevices, WicStock."));

            if (string.IsNullOrWhiteSpace(request.Status))
                return Ok(ApiResult<PrepareResult>.Fail("INVALID_INPUT", "Status is required."));

            var (valid, validValues) = ValidStatusForSource(request.Source, request.Status);
            if (!valid)
                return Ok(ApiResult<PrepareResult>.Fail("INVALID_STATUS",
                    $"Invalid status '{request.Status}' for {request.Source}. Valid: {validValues}."));

            var device = await _repo.GetDeviceBySourceAndIdAsync(request.Source, request.Id);
            if (device == null)
                return Ok(ApiResult<PrepareResult>.Fail("NOT_FOUND",
                    $"Device not found: source={request.Source}, id={request.Id}."));

            var result = new PrepareResult
            {
                Action          = "status-update",
                CurrentState    = device,
                ProposedChanges = new Dictionary<string, string?> { ["Status"] = request.Status },
                Warning         = device.Status == request.Status
                    ? "Status is already set to the requested value."
                    : null
            };

            return Ok(ApiResult<PrepareResult>.Success(result));
        }

        // POST /api/agent/v1/actions/prepare/location-update
        [HttpPost("prepare/location-update")]
        public async Task<IActionResult> PrepareLocationUpdate([FromBody] PrepareLocationUpdateRequest request)
        {
            if (!_repo.IsValidSource(request.Source))
                return Ok(ApiResult<PrepareResult>.Fail("INVALID_SOURCE",
                    $"Invalid source '{request.Source}'. Valid: ReturnDevices, LoanerDevices, WicStock."));

            if (string.IsNullOrWhiteSpace(request.DeviceLocation))
                return Ok(ApiResult<PrepareResult>.Fail("INVALID_INPUT", "DeviceLocation is required."));

            var device = await _repo.GetDeviceBySourceAndIdAsync(request.Source, request.Id);
            if (device == null)
                return Ok(ApiResult<PrepareResult>.Fail("NOT_FOUND",
                    $"Device not found: source={request.Source}, id={request.Id}."));

            var result = new PrepareResult
            {
                Action          = "location-update",
                CurrentState    = device,
                ProposedChanges = new Dictionary<string, string?> { ["DeviceLocation"] = request.DeviceLocation },
                Warning         = device.DeviceLocation == request.DeviceLocation
                    ? "DeviceLocation is already set to the requested value."
                    : null
            };

            return Ok(ApiResult<PrepareResult>.Success(result));
        }

        // POST /api/agent/v1/actions/prepare/assignment-update
        [HttpPost("prepare/assignment-update")]
        public async Task<IActionResult> PrepareAssignmentUpdate([FromBody] PrepareAssignmentUpdateRequest request)
        {
            if (!_repo.IsValidSource(request.Source))
                return Ok(ApiResult<PrepareResult>.Fail("INVALID_SOURCE",
                    $"Invalid source '{request.Source}'. Valid: ReturnDevices, LoanerDevices, WicStock."));

            if (request.Source != "LoanerDevices")
                return Ok(ApiResult<PrepareResult>.Fail("BLOCKED",
                    $"Assignment updates are only supported for LoanerDevices. Source '{request.Source}' does not have assignment fields."));

            var device = await _repo.GetDeviceBySourceAndIdAsync(request.Source, request.Id);
            if (device == null)
                return Ok(ApiResult<PrepareResult>.Fail("NOT_FOUND",
                    $"Device not found: source={request.Source}, id={request.Id}."));

            var proposed = new Dictionary<string, string?>();
            if (request.KidHandedTo != device.KidHandedTo)
                proposed["KidHandedTo"] = request.KidHandedTo;
            if (request.KID != device.KID)
                proposed["KID"] = request.KID;
            if (request.DateHandedOver?.ToString("o") != device.DateHandedOver?.ToString("o"))
                proposed["DateHandedOver"] = request.DateHandedOver?.ToString("o");

            var result = new PrepareResult
            {
                Action          = "assignment-update",
                CurrentState    = device,
                ProposedChanges = proposed,
                Warning         = proposed.Count == 0 ? "No changes detected." : null
            };

            return Ok(ApiResult<PrepareResult>.Success(result));
        }

        // POST /api/agent/v1/actions/prepare/return-mark
        [HttpPost("prepare/return-mark")]
        public async Task<IActionResult> PrepareReturnMark([FromBody] PrepareReturnMarkRequest request)
        {
            if (!_repo.IsValidSource(request.Source))
                return Ok(ApiResult<PrepareResult>.Fail("INVALID_SOURCE",
                    $"Invalid source '{request.Source}'. Valid: ReturnDevices, LoanerDevices, WicStock."));

            if (request.Source == "WicStock")
                return Ok(ApiResult<PrepareResult>.Fail("BLOCKED",
                    "WicStockDevice does not have a return concept. Use prepare/status-update to change availability."));

            var device = await _repo.GetDeviceBySourceAndIdAsync(request.Source, request.Id);
            if (device == null)
                return Ok(ApiResult<PrepareResult>.Fail("NOT_FOUND",
                    $"Device not found: source={request.Source}, id={request.Id}."));

            var targetStatus = request.Source switch
            {
                "ReturnDevices" => ReturnDeviceStatus.PickedUp.ToString(),
                "LoanerDevices" => LoanerStatus.Available.ToString(),
                _               => string.Empty
            };

            var result = new PrepareResult
            {
                Action          = "return-mark",
                CurrentState    = device,
                ProposedChanges = new Dictionary<string, string?> { ["Status"] = targetStatus },
                Warning         = device.Status == targetStatus
                    ? $"Device is already marked as {targetStatus}."
                    : null
            };

            return Ok(ApiResult<PrepareResult>.Success(result));
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static HashSet<string> AllowedFields(string source) => source switch
        {
            "ReturnDevices" => new HashSet<string> { "Status", "DeviceLocation", "WorkOrder", "PickupStatus", "Location" },
            "LoanerDevices" => new HashSet<string> { "Status", "DeviceLocation", "KidHandedTo", "KID", "DateHandedOver" },
            "WicStock"      => new HashSet<string> { "Status", "DeviceLocation", "SwapRITM" },
            _               => new HashSet<string>()
        };

        private static (bool valid, string validValues) ValidStatusForSource(string source, string status) =>
            source switch
            {
                "ReturnDevices" => (Enum.TryParse<ReturnDeviceStatus>(status, out _),
                    string.Join(", ", Enum.GetNames<ReturnDeviceStatus>())),
                "LoanerDevices" => (Enum.TryParse<LoanerStatus>(status, out _),
                    string.Join(", ", Enum.GetNames<LoanerStatus>())),
                "WicStock"      => (Enum.TryParse<WicStockStatus>(status, out _),
                    string.Join(", ", Enum.GetNames<WicStockStatus>())),
                _               => (false, string.Empty)
            };
    }
}
