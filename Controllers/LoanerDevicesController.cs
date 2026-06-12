using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using LaptopTracker.Helpers;
using LaptopTracker.Models;

namespace LaptopTracker.Controllers
{
    public class LoanerDevicesController : Controller
    {
        private readonly LaptopTrackerDbContext _context;

        public LoanerDevicesController(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchQuery, string? statusFilter, string? locationFilter, string? deviceTypeFilter, string? wicFilter)
        {
            var query = _context.LoanerDevices.Where(d => !d.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
                query = query.Where(d =>
                    d.SerialNumber.Contains(searchQuery) ||
                    d.RITM.Contains(searchQuery) ||
                    (d.KidHandedTo != null && d.KidHandedTo.Contains(searchQuery)) ||
                    (d.KID != null && d.KID.Contains(searchQuery)));

            if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<LoanerStatus>(statusFilter, out var sf))
                query = query.Where(d => d.Status == sf);

            if (!string.IsNullOrWhiteSpace(locationFilter))
                query = query.Where(d => d.DeviceLocation == locationFilter);

            if (!string.IsNullOrWhiteSpace(deviceTypeFilter))
                query = query.Where(d => d.DeviceType == deviceTypeFilter);

            if (!string.IsNullOrWhiteSpace(wicFilter))
                query = query.Where(d => d.WIC == wicFilter);

            var countryFilter = HttpContext.Session.GetString("CountryFilter");
            var countryLocations = LaptopTracker.Helpers.LocationList.GetLocationsByCountry(countryFilter);
            if (countryLocations != null)
                query = query.Where(d => countryLocations.Contains(d.DeviceLocation));

            var dbLocations = await _context.LoanerDevices
                .Where(d => !d.IsDeleted)
                .Select(d => d.DeviceLocation)
                .Where(l => l != null && l.Length > 0)
                .Distinct()
                .ToListAsync();

            var dbDeviceTypes = await _context.LoanerDevices
                .Where(d => !d.IsDeleted)
                .Select(d => d.DeviceType)
                .Where(t => t != null && t.Length > 0)
                .Distinct()
                .ToListAsync();

            var dbWicValues = await _context.LoanerDevices
                .Where(d => !d.IsDeleted && d.WIC != null && d.WIC.Length > 0)
                .Select(d => d.WIC!)
                .Distinct()
                .ToListAsync();

            var knownTypes = new[] { "Lenovo", "HP", "Dell", "Apple iPad" };

            ViewData["Locations"]        = dbLocations.OrderBy(l => l).ToList();
            ViewData["DeviceTypes"]      = dbDeviceTypes.Concat(knownTypes).Distinct().OrderBy(t => t).ToList();
            ViewData["WicValues"]        = dbWicValues.Concat(LocationList.SiteLocations).Distinct().OrderBy(w => w).ToList();
            ViewData["SearchQuery"]      = searchQuery;
            ViewData["StatusFilter"]     = statusFilter;
            ViewData["LocationFilter"]   = locationFilter;
            ViewData["DeviceTypeFilter"] = deviceTypeFilter;
            ViewData["WicFilter"]        = wicFilter;

            return View(await query.OrderByDescending(d => d.Date).ToListAsync());
        }

        [HttpGet]
        public IActionResult Create(string? serialNumber)
        {
            return View(new LoanerDevice { Date = DateTime.Today, SerialNumber = serialNumber ?? string.Empty });
        }

        [HttpPost]
        public async Task<IActionResult> Create(LoanerDevice device)
        {
            if (!ModelState.IsValid) return View(device);
            device.DeviceStateType = "Loaner Device";
            _context.LoanerDevices.Add(device);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> BulkCreate(
            string[] serials, string[] types, string[] ritms,
            DateTime date, string deviceLocation, string status,
            string? wic, string? kid, string? kidHandedTo, DateTime? dateHandedOver, string? userAddress)
        {
            var count = Math.Min(serials?.Length ?? 0, Math.Min(types?.Length ?? 0, ritms?.Length ?? 0));
            var statusEnum = Enum.TryParse<LoanerStatus>(status, out var s) ? s : LoanerStatus.Available;

            for (int i = 0; i < count; i++)
            {
                var sn = serials![i]?.Trim();
                if (string.IsNullOrWhiteSpace(sn)) continue;
                _context.LoanerDevices.Add(new LoanerDevice
                {
                    SerialNumber    = sn,
                    DeviceType      = types![i]?.Trim() ?? "Lenovo",
                    RITM            = ritms![i]?.Trim() ?? "",
                    Date            = date == default ? DateTime.Today : date,
                    DeviceLocation  = deviceLocation ?? "In WIC",
                    Status          = statusEnum,
                    WIC             = string.IsNullOrWhiteSpace(wic) ? null : wic,
                    KID             = string.IsNullOrWhiteSpace(kid) ? null : kid,
                    UserAddress     = string.IsNullOrWhiteSpace(userAddress) ? null : userAddress,
                    KidHandedTo     = string.IsNullOrWhiteSpace(kidHandedTo) ? null : kidHandedTo,
                    DateHandedOver  = dateHandedOver,
                    DeviceStateType = "Loaner Device"
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> BulkEdit([FromQuery] int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var idList = ids.ToList();
            var devices = await _context.LoanerDevices
                .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync();
            if (!devices.Any()) return RedirectToAction(nameof(Index));
            ViewData["Ids"] = ids;
            return View(devices);
        }

        [HttpPost]
        [ActionName("BulkEdit")]
        public async Task<IActionResult> BulkEditSave(int[] ids, string? status, string? deviceLocation)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var idList = ids.ToList();
            var devices = await _context.LoanerDevices
                .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync();

            foreach (var d in devices)
            {
                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LoanerStatus>(status, out var s))
                    d.Status = s;
                if (!string.IsNullOrWhiteSpace(deviceLocation))
                    d.DeviceLocation = deviceLocation;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var device = await _context.LoanerDevices.FindAsync(id);
            if (device == null) return NotFound();
            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, LoanerDevice model)
        {
            var device = await _context.LoanerDevices.FindAsync(id);
            if (device == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            device.SerialNumber    = model.SerialNumber;
            device.DeviceType      = model.DeviceType;
            device.DeviceStateType = "Loaner Device";
            device.RITM            = model.RITM;
            device.Date            = model.Date;
            device.DeviceLocation  = model.DeviceLocation;
            device.Status          = model.Status;
            device.KidHandedTo     = model.KidHandedTo;
            device.DateHandedOver  = model.DateHandedOver;
            device.WIC             = model.WIC;
            device.KID             = model.KID;
            device.UserAddress     = model.UserAddress;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ClearKID(int id)
        {
            var device = await _context.LoanerDevices.FindAsync(id);
            if (device != null)
            {
                device.KID = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await _context.LoanerDevices.FindAsync(id);
            if (device != null)
            {
                device.IsDeleted = true;
                device.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAll()
        {
            var devices = await _context.LoanerDevices.Where(d => !d.IsDeleted).ToListAsync();
            foreach (var d in devices) { d.IsDeleted = true; d.DeletedAt = DateTime.UtcNow; }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Scan(string serialNumber, string? deviceLocation, string? status,
            string? kidHandedTo, DateTime? dateHandedOver)
        {
            var sn = serialNumber.Trim().ToLower();
            var device = await _context.LoanerDevices
                .FirstOrDefaultAsync(d => d.SerialNumber.ToLower() == sn && !d.IsDeleted);

            if (device != null)
            {
                if (!string.IsNullOrWhiteSpace(deviceLocation)) device.DeviceLocation = deviceLocation;
                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LoanerStatus>(status, out var s)) device.Status = s;
                if (!string.IsNullOrWhiteSpace(kidHandedTo)) device.KidHandedTo = kidHandedTo;
                if (dateHandedOver.HasValue) device.DateHandedOver = dateHandedOver;
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
                if (await _context.LoanerDevices.AnyAsync(d => d.SerialNumber == sn)) continue;
                _context.LoanerDevices.Add(new LoanerDevice
                {
                    SerialNumber    = cols[0].Trim(),
                    DeviceType      = cols[1].Trim(),
                    DeviceStateType = cols[2].Trim(),
                    RITM            = cols[3].Trim(),
                    Date            = DateTime.TryParse(cols[4].Trim(), out var d) ? d : DateTime.UtcNow,
                    DeviceLocation  = cols[5].Trim(),
                    Status          = Enum.TryParse<LoanerStatus>(cols[6].Trim(), out var s) ? s : LoanerStatus.Available,
                    KidHandedTo     = cols.Length > 7 ? cols[7].Trim() : null,
                    DateHandedOver  = cols.Length > 8 && DateTime.TryParse(cols[8].Trim(), out var dho) ? dho : null,
                    WIC             = cols.Length > 9 ? cols[9].Trim() : null,
                    KID             = cols.Length > 10 ? cols[10].Trim() : null
                });
            }
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Import successful.";
            return RedirectToAction("Index");
        }
    }
}



