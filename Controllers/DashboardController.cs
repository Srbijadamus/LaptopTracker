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
            var returns  = await _context.ReturnDevices.ToListAsync();
            var wic      = await _context.WicStockDevices.ToListAsync();
            var loaners  = await _context.LoanerDevices.ToListAsync();

            int returnPending = returns.Count(d => d.Status == ReturnDeviceStatus.PendingPickup);

            ViewData["ReturnTotal"]       = returns.Count;
            ViewData["ReturnPending"]     = returnPending;
            ViewData["ReturnInTransit"]   = returns.Count(d => d.DeviceLocation == "In Transit");
            ViewData["ReturnWarning"]     = returnPending >= 5;

            ViewData["WicTotal"]          = wic.Count;
            ViewData["WicAvailable"]      = wic.Count(d => d.Status == WicStockStatus.Available);
            ViewData["WicInUse"]          = wic.Count(d => d.Status == WicStockStatus.NotAvailable);

            ViewData["LoanerTotal"]       = loaners.Count;
            ViewData["LoanerAvailable"]   = loaners.Count(d => d.Status == LoanerStatus.Available);
            ViewData["LoanerInUse"]       = loaners.Count(d => d.Status == LoanerStatus.NotAvailable);

            return View();
        }
    }
}
