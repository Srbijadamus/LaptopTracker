using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using LaptopTracker.Models;
using LaptopTracker.Services;

namespace LaptopTracker.Controllers
{
    public class HandoversController : Controller
    {
        private readonly LaptopTrackerDbContext _context;
        private readonly ICurrentUser _currentUser;

        public HandoversController(LaptopTrackerDbContext context, ICurrentUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // GET /Handovers
        public async Task<IActionResult> Index()
        {
            var countryFilter = HttpContext.Session.GetString("CountryFilter");
            var countryLocations = LaptopTracker.Helpers.LocationList.GetLocationsByCountry(countryFilter);

            var query = _context.Handovers.Include(h => h.Devices).AsQueryable();
            if (countryLocations != null)
                query = query.Where(h => countryLocations.Contains(h.Location));

            var handovers = await query
                .OrderByDescending(h => h.Date)
                .ToListAsync();
            return View(handovers);
        }

        // GET /Handovers/Create?source=ReturnDevices&ids=1&ids=2
        [HttpGet]
        public async Task<IActionResult> Create([FromQuery] string source, [FromQuery] int[] ids)
        {
            if (ids == null || ids.Length == 0)
                return RedirectToAction(nameof(Index));

            var devices = await LoadDevicesAsync(source, ids);
            if (!devices.Any())
                return RedirectToAction(nameof(Index));

            ViewData["Source"] = source;
            ViewData["Ids"] = ids;
            ViewData["Devices"] = devices;
            return View(new Handover { Date = DateTime.Now });
        }

        // POST /Handovers/Create
        [HttpPost]
        public async Task<IActionResult> Create(
            string name, string kid, string location, string? signature,
            string source, int[] ids)
        {
            var deviceInfos = await LoadDevicesAsync(source, ids);

            var handover = new Handover
            {
                Name = name?.Trim() ?? string.Empty,
                Kid = kid?.Trim() ?? string.Empty,
                Location = location?.Trim() ?? string.Empty,
                Date = DateTime.Now,
                Signature = string.IsNullOrWhiteSpace(signature) ? null : signature,
                Devices = deviceInfos.Select(d => new HandoverDevice
                {
                    DeviceId = d.Id,
                    DeviceType = source ?? string.Empty,
                    SerialNumber = d.SerialNumber,
                    WorkOrder = d.WorkOrder
                }).ToList()
            };

            _context.Handovers.Add(handover);

            // Handing a WIC Stock device over books it out of the WIC Bestand. Soft delete is the
            // same mechanism WicStockController.Delete uses, so the device leaves the stock list
            // (Index filters !IsDeleted), stays visible and restorable in Archive, and the audit
            // engine records it as SoftDelete. LoadDevicesAsync already skips deleted rows, so a
            // second handover of the same device finds no device to hand over.
            if (source == "WicStock" && deviceInfos.Count > 0)
            {
                var handedOverIds = deviceInfos.Select(d => d.Id).ToList();
                var stockDevices = await _context.WicStockDevices
                    .Where(d => handedOverIds.Contains(d.Id) && !d.IsDeleted)
                    .ToListAsync();

                foreach (var stockDevice in stockDevices)
                {
                    stockDevice.IsDeleted = true;
                    stockDevice.DeletedAt = DateTime.UtcNow;
                }
            }

            // A Return Device that is handed over leaves the Returns list by switching to the
            // HandedOver status (the Index filter excludes it). The row is untouched otherwise —
            // no soft delete, so it stays in the database and remains restorable/searchable.
            // The audit engine records the Status change; the explicit entries below add the
            // handover ID so the change is traceable back to this handover.
            var handedOverReturns = new List<(ReturnDevice Device, string OldStatus)>();
            if (source == "ReturnDevices" && deviceInfos.Count > 0)
            {
                var handedOverIds = deviceInfos.Select(d => d.Id).ToList();
                var returnDevices = await _context.ReturnDevices
                    .Where(d => handedOverIds.Contains(d.Id) && !d.IsDeleted)
                    .ToListAsync();

                foreach (var d in returnDevices)
                {
                    if (d.Status == ReturnDeviceStatus.HandedOver) continue;
                    handedOverReturns.Add((d, d.Status.ToString()));
                    d.Status = ReturnDeviceStatus.HandedOver;
                }
            }

            await _context.SaveChangesAsync();

            if (handedOverReturns.Count > 0)
            {
                foreach (var (device, oldStatus) in handedOverReturns)
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        Kid         = _currentUser.Kid ?? "ANONYMOUS",
                        Action      = "HandedOver",
                        EntityName  = nameof(ReturnDevice),
                        EntityId    = device.Id.ToString(),
                        EntityLabel = device.SerialNumber,
                        Changes     = JsonSerializer.Serialize(new
                        {
                            oldStatus,
                            newStatus  = ReturnDeviceStatus.HandedOver.ToString(),
                            handoverId = handover.Id
                        }),
                        Source      = "Web",
                        IpAddress   = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        Route       = HttpContext.Request.Path.Value,
                        RequestId   = HttpContext.TraceIdentifier
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = handover.Id });
        }

        // GET /Handovers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var handover = await _context.Handovers
                .Include(h => h.Devices)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (handover == null) return NotFound();

            // Lines written before the WorkOrder snapshot exists have NULL here. Resolve those from
            // the live source row so a re-print still shows the Work Order; IgnoreQueryFilters finds
            // sources that were soft-deleted by the handover. A source that is gone stays empty.
            await FillMissingWorkOrdersAsync(handover.Devices);
            return View(handover);
        }

        /// <summary>
        /// Fills WorkOrder on handover lines that have no snapshot (created before the column
        /// existed) from the live source row, including soft-deleted ones. Nothing is written back.
        /// </summary>
        private async Task FillMissingWorkOrdersAsync(List<HandoverDevice> devices)
        {
            var pending = devices.Where(d => string.IsNullOrWhiteSpace(d.WorkOrder)).ToList();
            if (pending.Count == 0) return;

            var byType = pending.GroupBy(d => d.DeviceType).ToDictionary(g => g.Key, g => g.Select(d => d.DeviceId).ToList());
            var resolved = new Dictionary<(string Type, int Id), string?>();

            void Add(string type, List<(int Id, string? WorkOrder)> rows)
            {
                foreach (var row in rows) resolved[(type, row.Id)] = row.WorkOrder;
            }

            if (byType.TryGetValue("ReturnDevices", out var returnIds))
                Add("ReturnDevices", await _context.ReturnDevices.IgnoreQueryFilters()
                    .Where(d => returnIds.Contains(d.Id))
                    .Select(d => new ValueTuple<int, string?>(d.Id, d.WorkOrder))
                    .ToListAsync());

            if (byType.TryGetValue("WicStock", out var wicIds))
                Add("WicStock", await _context.WicStockDevices.IgnoreQueryFilters()
                    .Where(d => wicIds.Contains(d.Id))
                    .Select(d => new ValueTuple<int, string?>(d.Id, d.WorkOrder))
                    .ToListAsync());

            // LoanerDevice has no Work Order field, so its lines stay empty by design.
            foreach (var line in pending)
            {
                if (resolved.TryGetValue((line.DeviceType, line.DeviceId), out var workOrder))
                    line.WorkOrder = workOrder;
            }
        }

        private async Task<List<DeviceSelectionInfo>> LoadDevicesAsync(string? source, int[]? ids)
        {
            if (ids == null || ids.Length == 0) return new List<DeviceSelectionInfo>();

            var idList = ids.ToList();

            if (source == "ReturnDevices")
            {
                return await _context.ReturnDevices
                    .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                    .Select(d => new DeviceSelectionInfo { Id = d.Id, SerialNumber = d.SerialNumber, DeviceType = d.DeviceType, WorkOrder = d.WorkOrder })
                    .ToListAsync();
            }
            if (source == "LoanerDevices")
            {
                return await _context.LoanerDevices
                    .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                    .Select(d => new DeviceSelectionInfo { Id = d.Id, SerialNumber = d.SerialNumber, DeviceType = d.DeviceType })
                    .ToListAsync();
            }
            if (source == "WicStock")
            {
                return await _context.WicStockDevices
                    .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                    .Select(d => new DeviceSelectionInfo { Id = d.Id, SerialNumber = d.SerialNumber, DeviceType = d.DeviceType, WorkOrder = d.WorkOrder })
                    .ToListAsync();
            }
            return new List<DeviceSelectionInfo>();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var handover = await _context.Handovers.FindAsync(id);
            if (handover != null)
            {
                _context.Handovers.Remove(handover);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}


