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

            var returnsQuery  = _context.ReturnDevices.Where(d => !d.IsDeleted);
            var wicQuery      = _context.WicStockDevices.Where(d => !d.IsDeleted);
            var loanersQuery  = _context.LoanerDevices.Where(d => !d.IsDeleted);

            if (countryLocations != null)
            {
                returnsQuery = returnsQuery.Where(d => d.Location != null && countryLocations.Contains(d.Location));
                wicQuery     = wicQuery.Where(d => countryLocations.Contains(d.DeviceLocation));
                loanersQuery = loanersQuery.Where(d => countryLocations.Contains(d.DeviceLocation));
            }

            var returns  = await returnsQuery.ToListAsync();
            var wic      = await wicQuery.ToListAsync();
            var loaners  = await loanersQuery.ToListAsync();

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

            ViewData["SwapPending"]       = wic.Count(d => d.SwapStatus == "SwapPending");
            ViewData["SwapReturnPending"] = wic.Count(d => d.SwapStatus == "SwapReturnPending");

            return View();
        }
    }
}


