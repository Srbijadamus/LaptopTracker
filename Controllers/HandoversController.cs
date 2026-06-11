using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using LaptopTracker.Models;

namespace LaptopTracker.Controllers
{
    public class HandoversController : Controller
    {
        private readonly LaptopTrackerDbContext _context;

        public HandoversController(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        // GET /Handovers
        public async Task<IActionResult> Index()
        {
            var handovers = await _context.Handovers
                .Include(h => h.Devices)
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
                    SerialNumber = d.SerialNumber
                }).ToList()
            };

            _context.Handovers.Add(handover);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = handover.Id });
        }

        // GET /Handovers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var handover = await _context.Handovers
                .Include(h => h.Devices)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (handover == null) return NotFound();
            return View(handover);
        }

        private async Task<List<DeviceSelectionInfo>> LoadDevicesAsync(string? source, int[]? ids)
        {
            if (ids == null || ids.Length == 0) return new List<DeviceSelectionInfo>();

            var idList = ids.ToList();

            if (source == "ReturnDevices")
            {
                return await _context.ReturnDevices
                    .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                    .Select(d => new DeviceSelectionInfo { Id = d.Id, SerialNumber = d.SerialNumber, DeviceType = d.DeviceType })
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
                    .Select(d => new DeviceSelectionInfo { Id = d.Id, SerialNumber = d.SerialNumber, DeviceType = d.DeviceType })
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

