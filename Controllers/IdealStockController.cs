using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using LaptopTracker.Helpers;
using LaptopTracker.Models;
using LaptopTracker.Services;
using ClosedXML.Excel;

namespace LaptopTracker.Controllers
{
    public class IdealStockController : Controller
    {
        private readonly LaptopTrackerDbContext _context;
        private readonly ICurrentUser _currentUser;
        private static readonly SizeClass[] AllClasses = { SizeClass.S, SizeClass.M, SizeClass.L, SizeClass.D };

        public IdealStockController(LaptopTrackerDbContext context, ICurrentUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IActionResult> Index()
        {
            var model = await BuildOverviewAsync();
            return View(model);
        }

        public async Task<IActionResult> Export()
        {
            var model = await BuildOverviewAsync();

            var rows = new List<(string Location, string ClassLabel, int ClassOrder, int InStock, int Reserved, int? Ideal, int? Difference, string Status)>();

            foreach (var group in model.Locations)
            {
                var displayLocation = group.IsUnassigned ? "Unassigned" : group.Location;

                foreach (var row in group.Rows)
                {
                    rows.Add((
                        displayLocation,
                        row.SizeClass.ToString(),
                        (int)row.SizeClass,
                        row.InStock,
                        row.Reserved,
                        row.IdealTarget,
                        row.Difference,
                        row.Status.ToString()));
                }

                rows.Add((
                    displayLocation,
                    "Unclassified (legacy)",
                    int.MaxValue,
                    group.Unclassified.InStock,
                    group.Unclassified.Reserved,
                    null,
                    null,
                    "-"));
            }

            var ordered = rows
                .OrderBy(r => r.Location, StringComparer.Ordinal)
                .ThenBy(r => r.ClassOrder)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ideal Stock");
            ws.Cell(1, 1).Value = "Location";
            ws.Cell(1, 2).Value = "Class";
            ws.Cell(1, 3).Value = "In Stock";
            ws.Cell(1, 4).Value = "Reserved";
            ws.Cell(1, 5).Value = "Ideal";
            ws.Cell(1, 6).Value = "Difference";
            ws.Cell(1, 7).Value = "Status";
            ws.Row(1).Style.Font.Bold = true;

            for (int i = 0; i < ordered.Count; i++)
            {
                var r = ordered[i];
                var excelRow = i + 2;
                ws.Cell(excelRow, 1).Value = r.Location;
                ws.Cell(excelRow, 2).Value = r.ClassLabel;
                ws.Cell(excelRow, 3).Value = r.InStock;
                ws.Cell(excelRow, 4).Value = r.Reserved;
                if (r.Ideal.HasValue) ws.Cell(excelRow, 5).Value = r.Ideal.Value; else ws.Cell(excelRow, 5).Value = "-";
                if (r.Difference.HasValue) ws.Cell(excelRow, 6).Value = r.Difference.Value; else ws.Cell(excelRow, 6).Value = "-";
                ws.Cell(excelRow, 7).Value = r.Status;
            }

            ws.Columns().AdjustToContents();
            ws.RangeUsed()?.SetAutoFilter();
            ws.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            _context.AuditLogs.Add(new AuditLog
            {
                Kid = _currentUser.Kid ?? "ANONYMOUS",
                Action = "Export",
                EntityName = "IdealStockOverview",
                EntityId = "",
                Source = "Web",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Route = HttpContext.Request.Path.Value,
                RequestId = HttpContext.TraceIdentifier
            });
            await _context.SaveChangesAsync();

            var fileName = $"IdealStock_export_{DateTime.Now:yyyy-MM-dd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public class SetTargetRequest
        {
            public string Location { get; set; } = string.Empty;
            public SizeClass SizeClass { get; set; }
            public int TargetQuantity { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetTarget([FromBody] SetTargetRequest req)
        {
            var location = (req?.Location ?? "").Trim();
            var sizeClass = req?.SizeClass ?? default;
            var targetQuantity = req?.TargetQuantity ?? 0;

            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(new { ok = false, error = "Location is required." });
            if (targetQuantity < 1 || targetQuantity > 8)
                return BadRequest(new { ok = false, error = "Target must be between 1 and 8." });

            using var tx = await _context.Database.BeginTransactionAsync();

            var row = await _context.IdealStockTargets
                .FirstOrDefaultAsync(t => t.Location == location && t.SizeClass == sizeClass);

            var who = string.IsNullOrWhiteSpace(_currentUser.Kid) ? "unknown" : _currentUser.Kid;
            var now = DateTime.UtcNow;

            if (row == null)
            {
                row = new IdealStockTarget { Location = location, SizeClass = sizeClass };
                _context.IdealStockTargets.Add(row);
            }
            row.TargetQuantity = targetQuantity;
            row.UpdatedAt = now;
            row.UpdatedBy = who;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new { ok = true, targetQuantity, updatedAt = now, updatedBy = who });
        }

        public class ClearTargetRequest
        {
            public string Location { get; set; } = string.Empty;
            public SizeClass SizeClass { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearTarget([FromBody] ClearTargetRequest req)
        {
            var location = (req?.Location ?? "").Trim();
            var sizeClass = req?.SizeClass ?? default;

            using var tx = await _context.Database.BeginTransactionAsync();

            var row = await _context.IdealStockTargets
                .FirstOrDefaultAsync(t => t.Location == location && t.SizeClass == sizeClass);
            if (row != null)
            {
                _context.IdealStockTargets.Remove(row);
                await _context.SaveChangesAsync();
            }
            await tx.CommitAsync();

            return Json(new { ok = true });
        }

        public class SetClassDDeviceTypeRequest
        {
            public string Location { get; set; } = string.Empty;
            public string DeviceType { get; set; } = string.Empty;
        }

        private static readonly string[] ClassDDeviceTypes = { "Lenovo", "Rugged tablet", "Microsoft Surface tablet", "Apple tablet" };

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetClassDDeviceType([FromBody] SetClassDDeviceTypeRequest req)
        {
            var location = (req?.Location ?? "").Trim();
            var deviceType = (req?.DeviceType ?? "").Trim();

            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(new { ok = false, error = "Location is required." });
            if (deviceType != "" && !ClassDDeviceTypes.Contains(deviceType))
                return BadRequest(new { ok = false, error = "Invalid device type." });

            using var tx = await _context.Database.BeginTransactionAsync();

            var row = await _context.IdealStockTargets
                .FirstOrDefaultAsync(t => t.Location == location && t.SizeClass == SizeClass.D);

            var who = string.IsNullOrWhiteSpace(_currentUser.Kid) ? "unknown" : _currentUser.Kid;
            var now = DateTime.UtcNow;

            if (deviceType == "")
            {
                if (row != null)
                {
                    row.DeviceTypeLabel = null;
                    row.UpdatedAt = now;
                    row.UpdatedBy = who;
                    if (row.TargetQuantity == 0)
                        _context.IdealStockTargets.Remove(row);
                }
            }
            else
            {
                if (row == null)
                {
                    row = new IdealStockTarget { Location = location, SizeClass = SizeClass.D };
                    _context.IdealStockTargets.Add(row);
                }
                row.DeviceTypeLabel = deviceType;
                row.UpdatedAt = now;
                row.UpdatedBy = who;
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new { ok = true, deviceType = deviceType == "" ? null : deviceType, updatedAt = now, updatedBy = who });
        }

        public class ApplyToAllRequest
        {
            public string SourceLocation { get; set; } = string.Empty;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyToAllLocations([FromBody] ApplyToAllRequest req)
        {
            var sourceLocation = (req?.SourceLocation ?? "").Trim();
            if (string.IsNullOrWhiteSpace(sourceLocation))
                return BadRequest(new { ok = false, error = "Source location is required." });

            var locations = await GetSiteLocationKeysAsync();
            if (!locations.Contains(sourceLocation))
                return BadRequest(new { ok = false, error = "Source location is required." });
            var destinations = locations.Where(l => l != sourceLocation).ToList();
            if (destinations.Count == 0)
                return Json(new { ok = true, applied = Array.Empty<object>(), previous = Array.Empty<object>() });

            var allTargets = await _context.IdealStockTargets
                .Where(t => t.Location == sourceLocation || destinations.Contains(t.Location))
                .ToListAsync();

            var sourceByClass = allTargets.Where(t => t.Location == sourceLocation)
                .ToDictionary(t => t.SizeClass, t => t.TargetQuantity);

            var who = string.IsNullOrWhiteSpace(_currentUser.Kid) ? "unknown" : _currentUser.Kid;
            var now = DateTime.UtcNow;
            var previous = new List<object>();

            using var tx = await _context.Database.BeginTransactionAsync();

            foreach (var dest in destinations)
            {
                foreach (var sizeClass in AllClasses)
                {
                    var existing = allTargets.FirstOrDefault(t => t.Location == dest && t.SizeClass == sizeClass);
                    previous.Add(new { location = dest, sizeClass = sizeClass.ToString(), previousQuantity = existing?.TargetQuantity });

                    var hasSource = sourceByClass.TryGetValue(sizeClass, out var sourceQty);
                    if (!hasSource)
                    {
                        if (existing != null) _context.IdealStockTargets.Remove(existing);
                        continue;
                    }

                    if (existing == null)
                    {
                        existing = new IdealStockTarget { Location = dest, SizeClass = sizeClass };
                        _context.IdealStockTargets.Add(existing);
                    }
                    existing.TargetQuantity = sourceQty;
                    existing.UpdatedAt = now;
                    existing.UpdatedBy = who;
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new { ok = true, appliedTo = destinations, previous });
        }

        public class RestoreItem
        {
            public string Location { get; set; } = string.Empty;
            public string SizeClass { get; set; } = string.Empty;
            public int? PreviousQuantity { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreTargets([FromBody] List<RestoreItem> items)
        {
            if (items == null || items.Count == 0)
                return Json(new { ok = true });

            var who = string.IsNullOrWhiteSpace(_currentUser.Kid) ? "unknown" : _currentUser.Kid;
            var now = DateTime.UtcNow;

            using var tx = await _context.Database.BeginTransactionAsync();

            foreach (var item in items)
            {
                if (!Enum.TryParse<SizeClass>(item.SizeClass, out var sizeClass)) continue;
                var location = (item.Location ?? "").Trim();

                var row = await _context.IdealStockTargets
                    .FirstOrDefaultAsync(t => t.Location == location && t.SizeClass == sizeClass);

                if (item.PreviousQuantity == null)
                {
                    if (row != null) _context.IdealStockTargets.Remove(row);
                    continue;
                }

                if (row == null)
                {
                    row = new IdealStockTarget { Location = location, SizeClass = sizeClass };
                    _context.IdealStockTargets.Add(row);
                }
                row.TargetQuantity = item.PreviousQuantity.Value;
                row.UpdatedAt = now;
                row.UpdatedBy = who;
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new { ok = true });
        }

        // Canonical seeded sites only, in stable order — every one renders as its own block
        // even with zero devices. Pseudo-locations (In WIC/In Transit, the old DeviceLocation
        // state values) are never grouping keys here.
        // Respects the same session CountryFilter the sidebar selector writes (CountryController.Set),
        // via the single LocationList.GetLocationsByCountry lookup — no separate filtering path.
        private async Task<List<string>> GetSiteLocationKeysAsync()
        {
            var keys = await _context.WicLocations
                .Where(l => l.IsActive)
                .OrderBy(l => l.DeviceLocationKey)
                .Select(l => l.DeviceLocationKey)
                .ToListAsync();

            var allowed = LocationList.GetLocationsByCountry(HttpContext.Session.GetString("CountryFilter"));
            if (allowed != null)
                keys = keys.Where(k => allowed.Contains(k)).ToList();

            return keys;
        }

        private async Task<IdealStockOverviewViewModel> BuildOverviewAsync()
        {
            var devices = await _context.WicStockDevices.Where(d => !d.IsDeleted).ToListAsync();
            var targets = await _context.IdealStockTargets.ToListAsync();
            var modelSizeByName = await _context.Models.ToDictionaryAsync(m => m.ModelName, m => m.SizeClass);

            // All seeded, active sites regardless of country — used to tell a genuine
            // unseeded/legacy Location apart from a seeded site that's merely excluded by the
            // country filter below. Comparing against the country-filtered list here would make
            // every other country's valid sites look like legacy locations once a filter is on.
            var allActiveKeys = await _context.WicLocations
                .Where(l => l.IsActive)
                .Select(l => l.DeviceLocationKey)
                .ToListAsync();

            var countryFilter = HttpContext.Session.GetString("CountryFilter");
            var allowedByCountry = LocationList.GetLocationsByCountry(countryFilter);
            var isCountryFiltered = allowedByCountry != null;

            var siteKeys = isCountryFiltered
                ? allActiveKeys.Where(k => allowedByCountry!.Contains(k)).OrderBy(k => k).ToList()
                : allActiveKeys.OrderBy(k => k).ToList();

            // A device Location that isn't one of the seeded sites at all (shouldn't happen once
            // the whitelist is enforced everywhere, but a stray legacy value still gets its own
            // visible block rather than being silently dropped from the reconciliation). These
            // have no WicLocations row, so no country to attribute them to — only surface them
            // when no specific country is selected.
            var extraLocationKeys = isCountryFiltered
                ? new List<string>()
                : devices
                    .Select(d => d.Location)
                    .Where(l => !string.IsNullOrWhiteSpace(l) && !allActiveKeys.Contains(l!))
                    .Select(l => l!)
                    .Distinct()
                    .OrderBy(l => l, StringComparer.Ordinal)
                    .ToList();

            var model = new IdealStockOverviewViewModel { TotalDeviceCount = devices.Count };

            foreach (var location in siteKeys)
            {
                var atLocation = devices.Where(d => d.Location == location).ToList();
                model.Locations.Add(BuildGroup(location, isUnassigned: false, isUnseededLegacy: false, atLocation, targets, modelSizeByName));
            }

            foreach (var location in extraLocationKeys)
            {
                var atLocation = devices.Where(d => d.Location == location).ToList();
                model.Locations.Add(BuildGroup(location, isUnassigned: false, isUnseededLegacy: true, atLocation, targets, modelSizeByName));
            }

            // Unassigned devices have no Location at all, so like true legacy locations they
            // can't be attributed to any country — only show them under "All".
            if (!isCountryFiltered)
            {
                var unassigned = devices.Where(d => string.IsNullOrWhiteSpace(d.Location)).ToList();
                model.Locations.Add(BuildGroup("__unassigned__", isUnassigned: true, isUnseededLegacy: false, unassigned, targets, modelSizeByName));
            }

            return model;
        }

        private static LocationStockGroup BuildGroup(
            string location, bool isUnassigned, bool isUnseededLegacy, List<WicStockDevice> atLocation,
            List<IdealStockTarget> targets, Dictionary<string, SizeClass> modelSizeByName)
        {
            var group = new LocationStockGroup
            {
                Location = location,
                IsUnassigned = isUnassigned,
                IsUnseededLegacy = isUnseededLegacy,
                DeviceCount = atLocation.Count
            };

            foreach (var sizeClass in AllClasses)
            {
                var inClass = atLocation
                    .Where(d => d.Model != null && modelSizeByName.TryGetValue(d.Model, out var sc) && sc == sizeClass)
                    .ToList();
                var inStock = inClass.Count(d => d.Status == WicStockStatus.Available && string.IsNullOrWhiteSpace(d.SwapStatus));
                var reserved = inClass.Count(d => !string.IsNullOrWhiteSpace(d.SwapStatus));

                // Unassigned devices have no real site, so a target lookup never applies to them.
                var target = isUnassigned ? null : targets.FirstOrDefault(t => t.Location == location && t.SizeClass == sizeClass);

                group.Rows.Add(new SizeClassStockRow
                {
                    SizeClass = sizeClass,
                    InStock = inStock,
                    Reserved = reserved,
                    IdealTarget = target?.TargetQuantity,
                    Difference = target == null ? (int?)null : inStock - target.TargetQuantity,
                    Status = target == null
                        ? StockStatus.Undefined
                        : (inStock - target.TargetQuantity) switch
                        {
                            0 => StockStatus.Ok,
                            > 0 => StockStatus.Surplus,
                            _ => StockStatus.Shortage
                        },
                    UpdatedAt = target?.UpdatedAt,
                    UpdatedBy = target?.UpdatedBy,
                    DeviceTypeLabel = sizeClass == SizeClass.D ? target?.DeviceTypeLabel : null
                });
            }

            var unclassified = atLocation.Where(d => d.Model == null || !modelSizeByName.ContainsKey(d.Model)).ToList();
            group.Unclassified.InStock = unclassified.Count(d => d.Status == WicStockStatus.Available && string.IsNullOrWhiteSpace(d.SwapStatus));
            group.Unclassified.Reserved = unclassified.Count(d => !string.IsNullOrWhiteSpace(d.SwapStatus));
            group.Unclassified.Total = unclassified.Count;
            group.Unclassified.Models = unclassified.Select(d => d.Model ?? "(no model recorded)").Distinct().OrderBy(x => x).ToList();

            return group;
        }
    }
}
