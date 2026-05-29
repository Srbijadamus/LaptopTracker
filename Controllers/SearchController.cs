using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;

namespace LaptopTracker.Controllers
{
    public class SearchController : Controller
    {
        private readonly LaptopTrackerDbContext _context;

        public SearchController(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? serialNumber)
        {
            ViewData["SerialNumber"] = serialNumber;
            if (string.IsNullOrWhiteSpace(serialNumber)) return View();

            var sn = serialNumber.Trim().ToLower();
            ViewData["ReturnResults"] = await _context.ReturnDevices
                .Where(d => d.SerialNumber.ToLower() == sn).ToListAsync();
            ViewData["WicResults"] = await _context.WicStockDevices
                .Where(d => d.SerialNumber.ToLower() == sn).ToListAsync();
            ViewData["LoanerResults"] = await _context.LoanerDevices
                .Where(d => d.SerialNumber.ToLower() == sn).ToListAsync();

            return View();
        }
    }
}
