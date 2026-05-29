using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;

namespace LaptopTracker.Controllers
{
    public class ArchiveController : Controller
    {
        private readonly LaptopTrackerDbContext _context;

        public ArchiveController(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);

            var oldLoaner = _context.LoanerDevices.Where(d => d.IsDeleted && d.DeletedAt < cutoff);
            var oldReturn = _context.ReturnDevices.Where(d => d.IsDeleted && d.DeletedAt < cutoff);
            var oldWic    = _context.WicStockDevices.Where(d => d.IsDeleted && d.DeletedAt < cutoff);
            _context.LoanerDevices.RemoveRange(oldLoaner);
            _context.ReturnDevices.RemoveRange(oldReturn);
            _context.WicStockDevices.RemoveRange(oldWic);
            await _context.SaveChangesAsync();

            ViewData["ArchivedLoaners"] = await _context.LoanerDevices
                .Where(d => d.IsDeleted)
                .OrderByDescending(d => d.DeletedAt)
                .ToListAsync();

            ViewData["ArchivedReturns"] = await _context.ReturnDevices
                .Where(d => d.IsDeleted)
                .OrderByDescending(d => d.DeletedAt)
                .ToListAsync();

            ViewData["ArchivedWic"] = await _context.WicStockDevices
                .Where(d => d.IsDeleted)
                .OrderByDescending(d => d.DeletedAt)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RestoreLoaner(int id)
        {
            var device = await _context.LoanerDevices.FindAsync(id);
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
            var device = await _context.ReturnDevices.FindAsync(id);
            if (device != null)
            {
                device.IsDeleted = false;
                device.DeletedAt = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RestoreWic(int id)
        {
            var device = await _context.WicStockDevices.FindAsync(id);
            if (device != null)
            {
                device.IsDeleted = false;
                device.DeletedAt = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
