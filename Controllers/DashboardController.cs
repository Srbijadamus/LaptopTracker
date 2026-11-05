using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using LaptopTracker.Models;

namespace LaptopTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly LaptopTrackerDbContext _context;

        public DashboardController(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var countryFilter = HttpContext.Session.GetString("CountryFilter");
            var countryLocations = LaptopTracker.Helpers.LocationList.GetLocationsByCountry(countryFilter);

            var returnsQ  = _context.ReturnDevices.Where(d => !d.IsDeleted);
            var wicQ      = _context.WicStockDevices.Where(d => !d.IsDeleted);
            var loanersQ  = _context.LoanerDevices.Where(d => !d.IsDeleted);

            if (countryLocations != null)
            {
                returnsQ  = returnsQ.Where(d  => d.Location != null && countryLocations.Contains(d.Location));
                wicQ      = wicQ.Where(d      => d.Location != null && countryLocations.Contains(d.Location));
                loanersQ  = loanersQ.Where(d  => d.WIC != null && countryLocations.Contains(d.WIC));
            }

            var returnCount  = await returnsQ.CountAsync(d => d.Status != ReturnDeviceStatus.HandedOver);
            var wicCount     = await wicQ.CountAsync();
            var loanerCount  = await loanersQ.CountAsync();

            // PendingPickup status only — "In Transit" (DeviceLocation) is no longer editable, so
            // it must not drive a visible number.
            var pendingCount = await returnsQ.CountAsync(d =>
                d.Status == ReturnDeviceStatus.PendingPickup);

            // Status distribution for doughnut chart
            var returnByStatus = await returnsQ
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var wicByStatus = await wicQ
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var loanerByStatus = await loanersQ
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusSlices = new List<StatusSlice>();
            foreach (var r in returnByStatus)
                statusSlices.Add(new StatusSlice(
                    r.Status == ReturnDeviceStatus.PendingPickup ? "Abholung ausstehend"
                        : r.Status == ReturnDeviceStatus.HandedOver ? "Übergeben" : "Abgeholt",
                    r.Count,
                    r.Status == ReturnDeviceStatus.PendingPickup ? "#FBBF24"
                        : r.Status == ReturnDeviceStatus.HandedOver ? "#64748B" : "#38BDF8"));
            foreach (var w in wicByStatus)
                statusSlices.Add(new StatusSlice(
                    w.Status == WicStockStatus.Available ? "WIC Verfügbar" : "WIC Nicht verf.",
                    w.Count,
                    w.Status == WicStockStatus.Available ? "#34D399" : "#8B949E"));
            foreach (var l in loanerByStatus)
                statusSlices.Add(new StatusSlice(
                    l.Status == LoanerStatus.Available ? "Leihger. Verfügbar" : "Leihger. In Nutzung",
                    l.Count,
                    l.Status == LoanerStatus.Available ? "#A78BFA" : "#6E7681"));

            // Devices by kind counts
            var devicesByKind = new List<KindCount>
            {
                new("Rückgaben",   returnCount),
                new("WIC-Bestand", wicCount),
                new("Leihgeräte",  loanerCount)
            };

            // Recent 5 devices (all three tables, sorted by date desc)
            var recentReturns = await returnsQ
                .OrderByDescending(d => d.Date)
                .Take(5)
                .Select(d => new RecentDevice(d.Id, d.SerialNumber, d.DeviceType, "Rückgaben", d.RITM,
                    d.Status == ReturnDeviceStatus.PendingPickup ? "Ausstehend"
                        : d.Status == ReturnDeviceStatus.HandedOver ? "Übergeben" : "Abgeholt",
                    d.Status == ReturnDeviceStatus.PendingPickup ? "#FBBF24"
                        : d.Status == ReturnDeviceStatus.HandedOver ? "#64748B" : "#38BDF8",
                    d.Date))
                .ToListAsync();

            var recentWic = await wicQ
                .OrderByDescending(d => d.Date)
                .Take(5)
                .Select(d => new RecentDevice(d.Id, d.SerialNumber, d.DeviceType, "WIC-Bestand", d.RITM,
                    d.Status == WicStockStatus.Available ? "Verfügbar" : "Nicht verf.",
                    d.Status == WicStockStatus.Available ? "#34D399" : "#8B949E",
                    d.Date))
                .ToListAsync();

            var recentLoaners = await loanersQ
                .OrderByDescending(d => d.Date)
                .Take(5)
                .Select(d => new RecentDevice(d.Id, d.SerialNumber, d.DeviceType, "Leihgeräte", d.RITM,
                    d.Status == LoanerStatus.Available ? "Verfügbar" : "In Nutzung",
                    d.Status == LoanerStatus.Available ? "#A78BFA" : "#6E7681",
                    d.Date))
                .ToListAsync();

            var recentDevices = recentReturns
                .Concat(recentWic)
                .Concat(recentLoaners)
                .OrderByDescending(d => d.Date)
                .Take(5)
                .ToList();

            // Recent audit entries — degrade gracefully if table absent
            var recentAudits = new List<RecentAudit>();
            var auditAvailable = false;
            try
            {
                recentAudits = await _context.AuditLogs
                    .OrderByDescending(a => a.TimestampUtc)
                    .Take(5)
                    .Select(a => new RecentAudit(a.Kid, a.Action, a.EntityLabel, a.TimestampUtc))
                    .ToListAsync();
                auditAvailable = true;
            }
            catch { /* table not yet migrated — hide panel */ }

            var vm = new DashboardViewModel
            {
                ReturnCount       = returnCount,
                WicStockCount     = wicCount,
                LoanerCount       = loanerCount,
                PendingCount      = pendingCount,
                StatusDistribution = statusSlices,
                DevicesByKind     = devicesByKind,
                RecentDevices     = recentDevices,
                RecentAudits      = recentAudits,
                AuditAvailable    = auditAvailable
            };

            return View(vm);
        }
    }
}
