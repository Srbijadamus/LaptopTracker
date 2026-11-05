using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using LaptopTracker.Models;
using LaptopTracker.Services;

namespace LaptopTracker.Controllers
{
    public class ArchiveController : Controller
    {
        private readonly LaptopTrackerDbContext _context;
        private readonly ICurrentUser _currentUser;

        public ArchiveController(LaptopTrackerDbContext context, ICurrentUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IActionResult> Index()
        {

            ViewData["ArchivedLoaners"] = await _context.LoanerDevices
                .IgnoreQueryFilters()
                .Where(d => d.IsDeleted)
                .OrderByDescending(d => d.DeletedAt)
                .ToListAsync();

            ViewData["ArchivedReturns"] = await _context.ReturnDevices
                .IgnoreQueryFilters()
                .Where(d => d.IsDeleted)
                .OrderByDescending(d => d.DeletedAt)
                .ToListAsync();

            ViewData["ArchivedWic"] = await _context.WicStockDevices
                .IgnoreQueryFilters()
                .Where(d => d.IsDeleted)
                .OrderByDescending(d => d.DeletedAt)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RestoreLoaner(int id)
        {
            var device = await _context.LoanerDevices
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device != null)
            {
                device.IsDeleted = false;
                device.DeletedAt = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RestoreReturn(int id)
        {
            var device = await _context.ReturnDevices
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
                return RedirectToAction(nameof(Index));

            // Safety: a soft-deleted Return whose linked WIC counterpart is currently active must
            // not be restorable — that would create two live records for one device.
            var activeWicCounterpart = await _context.WicStockDevices
                .IgnoreQueryFilters()
                .AnyAsync(w => w.ReturnDeviceId == id && !w.IsDeleted);
            if (activeWicCounterpart)
            {
                TempData["StatusMessage"] =
                    "Cannot restore: an active WIC Stock record already exists for this device. / Wiederherstellen nicht möglich: Für dieses Gerät existiert bereits ein aktiver WIC-Bestandseintrag.";
                return RedirectToAction(nameof(Index));
            }

            device.IsDeleted = false;
            device.DeletedAt = null;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RestoreWic(int id)
        {
            var device = await _context.WicStockDevices
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
                return RedirectToAction(nameof(Index));

            // Safety: a soft-deleted WIC row whose linked Return counterpart is currently active
            // must not be restorable — that would create two live records for one device.
            if (device.ReturnDeviceId.HasValue)
            {
                var activeReturnCounterpart = await _context.ReturnDevices
                    .IgnoreQueryFilters()
                    .AnyAsync(r => r.Id == device.ReturnDeviceId && !r.IsDeleted);
                if (activeReturnCounterpart)
                {
                    TempData["StatusMessage"] =
                        "Cannot restore: an active Return record already exists for this device. / Wiederherstellen nicht möglich: Für dieses Gerät existiert bereits ein aktiver Rückgabeeintrag.";
                    return RedirectToAction(nameof(Index));
                }
            }

            device.IsDeleted = false;
            device.DeletedAt = null;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> PurgeOld()
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);

            var oldLoaners = await _context.LoanerDevices
                .IgnoreQueryFilters()
                .Where(d => d.IsDeleted && d.DeletedAt < cutoff)
                .ToListAsync();

            var oldReturns = await _context.ReturnDevices
                .IgnoreQueryFilters()
                .Include(d => d.Photos)
                .Where(d => d.IsDeleted && d.DeletedAt < cutoff)
                .ToListAsync();

            var oldWic = await _context.WicStockDevices
                .IgnoreQueryFilters()
                .Where(d => d.IsDeleted && d.DeletedAt < cutoff)
                .ToListAsync();

            int total = oldLoaners.Count + oldReturns.Count + oldWic.Count;

            if (total > 0)
            {
                _context.LoanerDevices.RemoveRange(oldLoaners);
                _context.ReturnDevices.RemoveRange(oldReturns);
                _context.WicStockDevices.RemoveRange(oldWic);
                await _context.SaveChangesAsync();  // engine logs individual HardDelete rows

                _context.AuditLogs.Add(new AuditLog
                {
                    Kid        = _currentUser.Kid ?? "ANONYMOUS",
                    Action     = "PurgeOld",
                    EntityName = "BulkOp",
                    EntityId   = "",
                    Changes    = $"{{\"purgedCount\":{total},\"cutoffDays\":30}}",
                    Source     = "Web",
                    IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Route      = HttpContext.Request.Path.Value,
                    RequestId  = HttpContext.TraceIdentifier
                });
                await _context.SaveChangesAsync();
            }

            TempData["StatusMessage"] = total == 0
                ? "No items eligible for purge (none older than 30 days). / Keine Einträge älter als 30 Tage vorhanden."
                : $"Permanently purged {total} item{(total == 1 ? "" : "s")}. / {total} {(total == 1 ? "Eintrag" : "Einträge")} endgültig gelöscht.";

            return RedirectToAction(nameof(Index));
        }
    }
}


