using LaptopTracker.Data;
using LaptopTracker.Models.Agent;
using LaptopTracker.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LaptopTracker.Controllers.Agent.V1
{
    [ApiController]
    [Route("api/agent/v1/actions")]
    public class AgentCallController : ControllerBase
    {
        private readonly IAgentDeviceRepository _repo;
        private readonly IIdempotencyService    _idempotency;
        private readonly LaptopTrackerDbContext  _context;

        public AgentCallController(
            IAgentDeviceRepository repo,
            IIdempotencyService    idempotency,
            LaptopTrackerDbContext  context)
        {
            _repo        = repo;
            _idempotency = idempotency;
            _context     = context;
        }

        // POST /api/agent/v1/actions/call/status-update
        [HttpPost("call/status-update")]
        public async Task<IActionResult> CallStatusUpdate([FromBody] CallStatusUpdateRequest request)
        {
            const string endpoint = "call/status-update";

            var idempResult = await ResolveIdempotency(endpoint);
            if (idempResult.cached != null) return Content(idempResult.cached, "application/json");
            if (idempResult.missing) return BadRequest(new { error = "Idempotency-Key header is required for call endpoints." });
            var requestId = idempResult.requestId!;

            if (!_repo.IsValidSource(request.Source))
                return await Finalize(requestId, endpoint, request.Source, request.Id,
                    ApiResult<DeviceDto>.Fail("INVALID_SOURCE",
                        $"Invalid source '{request.Source}'. Valid: ReturnDevices, LoanerDevices, WicStock."));

            if (string.IsNullOrWhiteSpace(request.Status))
                return await Finalize(requestId, endpoint, request.Source, request.Id,
                    ApiResult<DeviceDto>.Fail("INVALID_INPUT", "Status is required."));

            var (ok, device, error) = await _repo.UpdateStatusAsync(request.Source, request.Id, request.Status);
            return await Finalize(requestId, endpoint, request.Source, request.Id,
                ok
                    ? ApiResult<DeviceDto>.Success(device!)
                    : ApiResult<DeviceDto>.Fail(error!.StartsWith("BLOCKED") ? "BLOCKED" : "EXECUTION_ERROR", error!));
        }

        // POST /api/agent/v1/actions/call/location-update
        [HttpPost("call/location-update")]
        public async Task<IActionResult> CallLocationUpdate([FromBody] CallLocationUpdateRequest request)
        {
            const string endpoint = "call/location-update";

            var idempResult = await ResolveIdempotency(endpoint);
            if (idempResult.cached != null) return Content(idempResult.cached, "application/json");
            if (idempResult.missing) return BadRequest(new { error = "Idempotency-Key header is required for call endpoints." });
            var requestId = idempResult.requestId!;

            if (!_repo.IsValidSource(request.Source))
                return await Finalize(requestId, endpoint, request.Source, request.Id,
                    ApiResult<DeviceDto>.Fail("INVALID_SOURCE",
                        $"Invalid source '{request.Source}'. Valid: ReturnDevices, LoanerDevices, WicStock."));

            if (string.IsNullOrWhiteSpace(request.DeviceLocation))
                return await Finalize(requestId, endpoint, request.Source, request.Id,
                    ApiResult<DeviceDto>.Fail("INVALID_INPUT", "DeviceLocation is required."));

            var (ok, device, error) = await _repo.UpdateLocationAsync(request.Source, request.Id, request.DeviceLocation);
            return await Finalize(requestId, endpoint, request.Source, request.Id,
                ok
                    ? ApiResult<DeviceDto>.Success(device!)
                    : ApiResult<DeviceDto>.Fail(error!.StartsWith("BLOCKED") ? "BLOCKED" : "EXECUTION_ERROR", error!));
        }

        // POST /api/agent/v1/actions/call/assignment-update
        [HttpPost("call/assignment-update")]
        public async Task<IActionResult> CallAssignmentUpdate([FromBody] CallAssignmentUpdateRequest request)
        {
            const string endpoint = "call/assignment-update";

            var idempResult = await ResolveIdempotency(endpoint);
            if (idempResult.cached != null) return Content(idempResult.cached, "application/json");
            if (idempResult.missing) return BadRequest(new { error = "Idempotency-Key header is required for call endpoints." });
            var requestId = idempResult.requestId!;

            if (!_repo.IsValidSource(request.Source))
                return await Finalize(requestId, endpoint, request.Source, request.Id,
                    ApiResult<DeviceDto>.Fail("INVALID_SOURCE",
                        $"Invalid source '{request.Source}'. Valid: ReturnDevices, LoanerDevices, WicStock."));

            var (ok, device, error) = await _repo.UpdateAssignmentAsync(
                request.Source, request.Id, request.KidHandedTo, request.KID, request.DateHandedOver);
            return await Finalize(requestId, endpoint, request.Source, request.Id,
                ok
                    ? ApiResult<DeviceDto>.Success(device!)
                    : ApiResult<DeviceDto>.Fail(error!.StartsWith("BLOCKED") ? "BLOCKED" : "EXECUTION_ERROR", error!));
        }

        // POST /api/agent/v1/actions/call/return
        [HttpPost("call/return")]
        public async Task<IActionResult> CallReturn([FromBody] CallReturnRequest request)
        {
            const string endpoint = "call/return";

            var idempResult = await ResolveIdempotency(endpoint);
            if (idempResult.cached != null) return Content(idempResult.cached, "application/json");
            if (idempResult.missing) return BadRequest(new { error = "Idempotency-Key header is required for call endpoints." });
            var requestId = idempResult.requestId!;

            if (!_repo.IsValidSource(request.Source))
                return await Finalize(requestId, endpoint, request.Source, request.Id,
                    ApiResult<DeviceDto>.Fail("INVALID_SOURCE",
                        $"Invalid source '{request.Source}'. Valid: ReturnDevices, LoanerDevices, WicStock."));

            var (ok, device, error) = await _repo.MarkReturnedAsync(request.Source, request.Id);
            return await Finalize(requestId, endpoint, request.Source, request.Id,
                ok
                    ? ApiResult<DeviceDto>.Success(device!)
                    : ApiResult<DeviceDto>.Fail(error!.StartsWith("BLOCKED") ? "BLOCKED" : "EXECUTION_ERROR", error!));
        }

        // ── Shared idempotency + audit helpers ────────────────────────────────────

        private async Task<(string? cached, bool missing, string? requestId)> ResolveIdempotency(string endpoint)
        {
            if (!Request.Headers.TryGetValue("Idempotency-Key", out var keys) ||
                string.IsNullOrWhiteSpace(keys.ToString()))
            {
                return (null, true, null);
            }

            var requestId = keys.ToString().Trim();
            var cached    = await _idempotency.GetCachedResponseAsync(requestId, endpoint);
            return (cached, false, requestId);
        }

        private async Task<IActionResult> Finalize<T>(
            string requestId, string endpoint, string source, int targetId,
            ApiResult<T> result)
        {
            var responseJson = JsonSerializer.Serialize(result);

            await _idempotency.StoreResponseAsync(requestId, endpoint, responseJson);

            _context.AgentActionAudits.Add(new AgentActionAudit
            {
                RequestId  = requestId,
                Endpoint   = endpoint,
                Source     = source,
                TargetId   = targetId,
                ActionJson = JsonSerializer.Serialize(new { endpoint, source, targetId }),
                Result     = result.Ok ? "SUCCESS" : $"FAILED:{result.Error?.Code}",
                CreatedAt  = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Content(responseJson, "application/json");
        }
    }
}
