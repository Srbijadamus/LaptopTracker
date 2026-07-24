using LaptopTracker.Data;
using LaptopTracker.Models.Agent;
using LaptopTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            return await FinalizeWithOp(requestId, endpoint, request.Source, request.Id,
                () => _repo.UpdateStatusAsync(request.Source, request.Id, request.Status));
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

            return await FinalizeWithOp(requestId, endpoint, request.Source, request.Id,
                () => _repo.UpdateLocationAsync(request.Source, request.Id, request.DeviceLocation));
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

            return await FinalizeWithOp(requestId, endpoint, request.Source, request.Id,
                () => _repo.UpdateAssignmentAsync(request.Source, request.Id,
                    request.KidHandedTo, request.KID, request.DateHandedOver));
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

            return await FinalizeWithOp(requestId, endpoint, request.Source, request.Id,
                () => _repo.MarkReturnedAsync(request.Source, request.Id));
        }

        // ── Shared helpers ────────────────────────────────────────────────────────

        private async Task<(string? cached, bool missing, string? requestId)> ResolveIdempotency(string endpoint)
        {
            if (!Request.Headers.TryGetValue("Idempotency-Key", out var keys) ||
                string.IsNullOrWhiteSpace(keys.ToString()))
                return (null, true, null);

            var requestId = keys.ToString().Trim();
            var cached    = await _idempotency.GetCachedResponseAsync(requestId, endpoint);
            return (cached, false, requestId);
        }

        // Finalize a business operation + idempotency + audit in a single transaction.
        // Errors are never cached. Concurrent duplicate key → return cached response (200, not 500).
        private async Task<IActionResult> FinalizeWithOp(
            string requestId, string endpoint, string source, int targetId,
            Func<Task<(bool ok, DeviceDto? device, string? error)>> executeAsync)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var (ok, device, error) = await executeAsync();
                var result = ok
                    ? ApiResult<DeviceDto>.Success(device!)
                    : ApiResult<DeviceDto>.Fail(
                        error!.StartsWith("BLOCKED") ? "BLOCKED" : "EXECUTION_ERROR", error!);

                return await CommitFinalize(tx, requestId, endpoint, source, targetId, result);
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                await tx.RollbackAsync();
                var cached = await _idempotency.GetCachedResponseAsync(requestId, endpoint);
                return Content(cached ?? "{\"ok\":false,\"error\":{\"code\":\"CONFLICT\"}}", "application/json");
            }
        }

        // Finalize a validation-only result (no business op; don't wrap in transaction — nothing to roll back).
        // Validation errors are not cached.
        private async Task<IActionResult> Finalize<T>(
            string requestId, string endpoint, string source, int targetId,
            ApiResult<T> result)
        {
            _context.AgentActionAudits.Add(BuildAudit(requestId, endpoint, source, targetId, result));
            await _context.SaveChangesAsync();
            return Content(JsonSerializer.Serialize(result), "application/json");
        }

        // Inner helper: store idempotency record (success only) + audit, commit.
        private async Task<IActionResult> CommitFinalize(
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx,
            string requestId, string endpoint, string source, int targetId,
            ApiResult<DeviceDto> result)
        {
            var responseJson = JsonSerializer.Serialize(result);

            if (result.Ok)
            {
                _context.IdempotencyRecords.Add(new IdempotencyRecord
                {
                    RequestId    = requestId,
                    Endpoint     = endpoint,
                    ResponseJson = responseJson,
                    CreatedAt    = DateTime.UtcNow
                });
            }

            _context.AgentActionAudits.Add(BuildAudit(requestId, endpoint, source, targetId, result));
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Content(responseJson, "application/json");
        }

        private static AgentActionAudit BuildAudit<T>(
            string requestId, string endpoint, string source, int targetId, ApiResult<T> result) =>
            new AgentActionAudit
            {
                RequestId  = requestId,
                Endpoint   = endpoint,
                Source     = source,
                TargetId   = targetId,
                ActionJson = JsonSerializer.Serialize(new { endpoint, source, targetId }),
                Result     = result.Ok ? "SUCCESS" : $"FAILED:{result.Error?.Code}",
                CreatedAt  = DateTime.UtcNow
            };

        private static bool IsDuplicateKey(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("Violation of UNIQUE KEY constraint") == true ||
            ex.InnerException?.Message.Contains("duplicate key") == true ||
            ex.InnerException?.Message.Contains("Cannot insert duplicate key") == true;
    }
}
