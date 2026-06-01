using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using LaptopTracker.Helpers;
using LaptopTracker.Models;

namespace LaptopTracker.Controllers
{
    public class ReturnDevicesController : Controller
    {
        private readonly LaptopTrackerDbContext _context;

        public ReturnDevicesController(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchQuery, string? statusFilter, string? locationFilter, string? deviceTypeFilter, bool hasWorkOrder)
        {
            var query = _context.ReturnDevices.Where(d => !d.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
                query = query.Where(d =>
                    d.SerialNumber.Contains(searchQuery) ||
                    d.RITM.Contains(searchQuery) ||
                    (d.WorkOrder != null && d.WorkOrder.Contains(searchQuery)));

            if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<ReturnDeviceStatus>(statusFilter, out var sf))
                query = query.Where(d => d.Status == sf);

            if (!string.IsNullOrWhiteSpace(locationFilter))
                query = query.Where(d =>
                    d.DeviceLocation == locationFilter ||
                    (d.Location != null && d.Location == locationFilter));

            if (!string.IsNullOrWhiteSpace(deviceTypeFilter))
                query = query.Where(d => d.DeviceType == deviceTypeFilter);

            if (hasWorkOrder)
                query = query.Where(d => d.WorkOrder != null && d.WorkOrder != "" && d.WorkOrder != "NA");

            var dbLocations = await _context.ReturnDevices
                .Where(d => !d.IsDeleted)
                .Select(d => d.DeviceLocation)
                .Where(l => l != null && l.Length > 0)
                .Distinct()
                .ToListAsync();

            var locationFieldValues = await _context.ReturnDevices
                .Where(d => !d.IsDeleted && d.Location != null && d.Location.Length > 0)
                .Select(d => d.Location!)
                .Distinct()
                .ToListAsync();

            var dbDeviceTypes = await _context.ReturnDevices
                .Where(d => !d.IsDeleted)
                .Select(d => d.DeviceType)
                .Where(t => t != null && t.Length > 0)
                .Distinct()
                .ToListAsync();

            var knownTypes = new[] { "Lenovo", "HP", "Dell", "Apple iPad" };

            ViewData["Locations"]        = dbLocations.Concat(locationFieldValues).Distinct().OrderBy(l => l).ToList();
            ViewData["DeviceTypes"]      = dbDeviceTypes.Concat(knownTypes).Distinct().OrderBy(t => t).ToList();
            ViewData["SearchQuery"]      = searchQuery;
            ViewData["StatusFilter"]     = statusFilter;
            ViewData["LocationFilter"]   = locationFilter;
            ViewData["DeviceTypeFilter"] = deviceTypeFilter;
            ViewData["HasWorkOrder"]     = hasWorkOrder;

            var allDevices = await query.OrderByDescending(d => d.Date).ToListAsync();
            var activeDevices   = allDevices.Where(d => d.Status != ReturnDeviceStatus.PickedUp).ToList();
            var collectedDevices = allDevices.Where(d => d.Status == ReturnDeviceStatus.PickedUp).ToList();
            ViewData["CollectedDevices"] = collectedDevices;
            return View(activeDevices);
        }

        [HttpGet]
        public IActionResult Create(string? serialNumber)
        {
            return View(new ReturnDevice { Date = DateTime.Today, SerialNumber = serialNumber ?? string.Empty });
        }

        [HttpPost]
        public async Task<IActionResult> Create(ReturnDevice device)
        {
            if (!ModelState.IsValid) return View(device);
            device.DeviceStateType = "Return";
            _context.ReturnDevices.Add(device);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> BulkCreate(
            string[] serials, string[] types, string[] ritms,
            DateTime date, string deviceLocation, string status,
            string? workOrder, string? pickupStatus, string? location,
            string? chargerReturned, string? powerCableReturned)
        {
            var count = Math.Min(serials?.Length ?? 0, Math.Min(types?.Length ?? 0, ritms?.Length ?? 0));
            var statusEnum = Enum.TryParse<ReturnDeviceStatus>(status, out var s) ? s : ReturnDeviceStatus.PendingPickup;
            bool? charger = string.IsNullOrEmpty(chargerReturned) ? null : bool.TryParse(chargerReturned, out var cr) ? cr : (bool?)null;
            bool? power   = string.IsNullOrEmpty(powerCableReturned) ? null : bool.TryParse(powerCableReturned, out var pcr) ? pcr : (bool?)null;

            for (int i = 0; i < count; i++)
            {
                var sn = serials![i]?.Trim();
                if (string.IsNullOrWhiteSpace(sn)) continue;
                _context.ReturnDevices.Add(new ReturnDevice
                {
                    SerialNumber       = sn,
                    DeviceType         = types![i]?.Trim() ?? "Lenovo",
                    RITM               = ritms![i]?.Trim() ?? "",
                    Date               = date == default ? DateTime.Today : date,
                    DeviceLocation     = deviceLocation ?? "In WIC",
                    Status             = statusEnum,
                    WorkOrder          = string.IsNullOrWhiteSpace(workOrder) ? null : workOrder,
                    PickupStatus       = string.IsNullOrWhiteSpace(pickupStatus) ? null : pickupStatus,
                    Location           = string.IsNullOrWhiteSpace(location) ? null : location,
                    ChargerReturned    = charger,
                    PowerCableReturned = power,
                    DeviceStateType    = "Return"
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdateWorkOrder(int[] ids, string workOrder)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var idList = ids.ToList();
            var devices = await _context.ReturnDevices
                .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync();
            foreach (var d in devices)
                d.WorkOrder = workOrder;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> BulkEdit([FromQuery] int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var idList = ids.ToList();
            var devices = await _context.ReturnDevices
                .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync();
            if (!devices.Any()) return RedirectToAction(nameof(Index));
            ViewData["Ids"] = ids;
            return View(devices);
        }

        [HttpPost]
        [ActionName("BulkEdit")]
        public async Task<IActionResult> BulkEditSave(int[] ids, string? status, string? deviceLocation, string? workOrder, string? location)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var idList = ids.ToList();
            var devices = await _context.ReturnDevices
                .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync();

            foreach (var d in devices)
            {
                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReturnDeviceStatus>(status, out var s))
                    d.Status = s;
                if (!string.IsNullOrWhiteSpace(deviceLocation))
                    d.DeviceLocation = deviceLocation;
                if (!string.IsNullOrWhiteSpace(workOrder))
                    d.WorkOrder = workOrder;
                if (!string.IsNullOrWhiteSpace(location))
                    d.Location = location;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var device = await _context.ReturnDevices.FindAsync(id);
            if (device == null) return NotFound();
            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ReturnDevice model)
        {
            var device = await _context.ReturnDevices.FindAsync(id);
            if (device == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            device.SerialNumber       = model.SerialNumber;
            device.DeviceType         = model.DeviceType;
            device.DeviceStateType    = "Return";
            device.RITM               = model.RITM;
            device.Date               = model.Date;
            device.DeviceLocation     = model.DeviceLocation;
            device.Status             = model.Status;
            device.WorkOrder          = model.WorkOrder;
            device.PickupStatus       = model.PickupStatus;
            device.Location           = model.Location;
            device.ChargerReturned    = model.ChargerReturned;
            device.PowerCableReturned = model.PowerCableReturned;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await _context.ReturnDevices.FindAsync(id);
            if (device != null)
            {
                device.IsDeleted = true;
                device.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids != null && ids.Length > 0)
            {
                var idList = ids.ToList();
                var devices = await _context.ReturnDevices
                    .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                    .ToListAsync();
                foreach (var d in devices) { d.IsDeleted = true; d.DeletedAt = DateTime.UtcNow; }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAll()
        {
            var devices = await _context.ReturnDevices.Where(d => !d.IsDeleted).ToListAsync();
            foreach (var d in devices) { d.IsDeleted = true; d.DeletedAt = DateTime.UtcNow; }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Scan(string serialNumber, string? deviceLocation, string? status,
            string? workOrder, string? pickupStatus, string? location)
        {
            var sn = serialNumber.Trim().ToLower();
            var device = await _context.ReturnDevices
                .FirstOrDefaultAsync(d => d.SerialNumber.ToLower() == sn && !d.IsDeleted);

            if (device != null)
            {
                if (!string.IsNullOrWhiteSpace(deviceLocation)) device.DeviceLocation = deviceLocation;
                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReturnDeviceStatus>(status, out var s)) device.Status = s;
                if (!string.IsNullOrWhiteSpace(workOrder)) device.WorkOrder = workOrder;
                if (!string.IsNullOrWhiteSpace(pickupStatus)) device.PickupStatus = pickupStatus;
                if (!string.IsNullOrWhiteSpace(location)) device.Location = location;
                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["StatusMessage"] = $"Device not found: {serialNumber.Trim()}. Please create a new record.";
            }

            return RedirectToAction("Index", "Search", new { serialNumber = serialNumber.Trim() });
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["StatusMessage"] = "No file selected.";
                return RedirectToAction("Index");
            }
            using var reader = new StreamReader(csvFile.OpenReadStream());
            await reader.ReadLineAsync();
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) continue;
                var cols = CsvParser.ParseLine(line).ToArray();
                if (cols.Length < 7) continue;
                var sn = cols[0].Trim();
                if (await _context.ReturnDevices.AnyAsync(d => d.SerialNumber == sn)) continue;
                _context.ReturnDevices.Add(new ReturnDevice
                {
                    SerialNumber    = cols[0].Trim(),
                    DeviceType      = cols[1].Trim(),
                    DeviceStateType = cols[2].Trim(),
                    RITM            = cols[3].Trim(),
                    Date            = DateTime.TryParse(cols[4].Trim(), out var d) ? d : DateTime.UtcNow,
                    DeviceLocation  = cols[5].Trim(),
                    Status          = Enum.TryParse<ReturnDeviceStatus>(cols[6].Trim(), out var s) ? s : ReturnDeviceStatus.PendingPickup,
                    WorkOrder       = cols.Length > 7 ? cols[7].Trim() : null,
                    PickupStatus    = cols.Length > 8 ? cols[8].Trim() : null,
                    Location        = cols.Length > 9 ? cols[9].Trim() : null,
                    ChargerReturned    = cols.Length > 10 && bool.TryParse(cols[10].Trim(), out var cr) ? cr : null,
                    PowerCableReturned = cols.Length > 11 && bool.TryParse(cols[11].Trim(), out var pcr) ? pcr : null
                });
            }
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Import successful.";
            return RedirectToAction("Index");
        }
    }
}



