using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using LaptopTracker.Helpers;
using LaptopTracker.Models;
using LaptopTracker.Services;

namespace LaptopTracker.Controllers
{
    public class ReturnDevicesController : Controller
    {
        private readonly LaptopTrackerDbContext _context;
        private readonly ReturnDocumentService _docSvc;
        private readonly ICurrentUser _currentUser;

        public ReturnDevicesController(LaptopTrackerDbContext context,
            ReturnDocumentService docSvc, ICurrentUser currentUser)
        {
            _context = context;
            _docSvc  = docSvc;
            _currentUser = currentUser;
        }

        public async Task<IActionResult> Index(string? searchQuery, string? statusFilter, string? locationFilter, string? deviceTypeFilter, string? hasWorkOrder)
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

            if (hasWorkOrder == "true")
                query = query.Where(d => d.WorkOrder != null && d.WorkOrder != "" && d.WorkOrder != "NA");
            else if (hasWorkOrder == "false")
                query = query.Where(d => string.IsNullOrEmpty(d.WorkOrder) || d.WorkOrder == "NA");

            var countryFilter = HttpContext.Session.GetString("CountryFilter");
            var countryLocations = LaptopTracker.Helpers.LocationList.GetLocationsByCountry(countryFilter);
            if (countryLocations != null)
                query = query.Where(d => d.Location != null && countryLocations.Contains(d.Location));

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

            var knownTypes = new[] { "Lenovo", "HP", "Dell", "Apple iPad", "Desktop", "Mini Desktop" };

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
        public IActionResult Create(string? serialNumber, string? prefillLocation, string? prefillDeviceType,
            string? prefillDeviceLocation, string? prefillDate, string? prefillRitm,
            string? prefillStatus, string? prefillWorkOrder)
        {
            var d = new ReturnDevice
            {
                Date           = DateTime.TryParse(prefillDate, out var dt) ? dt : DateTime.Today,
                SerialNumber   = serialNumber ?? string.Empty,
                Location       = prefillLocation,
                DeviceType     = string.IsNullOrEmpty(prefillDeviceType) ? "Lenovo" : prefillDeviceType,
                DeviceLocation = string.IsNullOrEmpty(prefillDeviceLocation) ? "In WIC" : prefillDeviceLocation,
                RITM           = prefillRitm ?? string.Empty,
                WorkOrder      = prefillWorkOrder
            };
            ViewData["StatusChoice"] = prefillStatus;
            return View(d);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ReturnDevice device, List<IFormFile>? damagePhotos, string? signatureData, string? statusChoice, string? addAnother)
        {
            if (statusChoice == "WicStock")
            {
                _context.WicStockDevices.Add(new WicStockDevice
                {
                    SerialNumber    = device.SerialNumber,
                    DeviceType      = device.DeviceType,
                    DeviceStateType = "WIC Stock",
                    RITM            = device.RITM,
                    Date            = device.Date,
                    DeviceLocation  = device.DeviceLocation,
                    Status          = WicStockStatus.Available
                });
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "WicStock");
            }

            if (string.IsNullOrWhiteSpace(device.Location)) ModelState.AddModelError("Location", "Location is required.");
            if (!ModelState.IsValid) return View(device);
            device.Status          = Enum.TryParse<ReturnDeviceStatus>(statusChoice, out var sc) ? sc : ReturnDeviceStatus.PendingPickup;
            device.DeviceStateType = "Return";
            _context.ReturnDevices.Add(device);
            await _context.SaveChangesAsync();

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadDir);
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var photoAdded = false;

            if (damagePhotos != null && damagePhotos.Count > 0)
            {
                foreach (var file in damagePhotos)
                {
                    if (file.Length == 0 || file.Length > 10 * 1024 * 1024) continue;
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowed.Contains(ext)) continue;
                    var uniqueName = $"{Guid.NewGuid()}{ext}";
                    var savePath = Path.Combine(uploadDir, uniqueName);
                    using var fs = new FileStream(savePath, FileMode.Create);
                    await file.CopyToAsync(fs);
                    _context.ReturnDevicePhotos.Add(new ReturnDevicePhoto
                    {
                        ReturnDeviceId   = device.Id,
                        FilePath         = $"/uploads/{uniqueName}",
                        OriginalFileName = $"damage-{file.FileName}",
                        UploadedAt       = DateTime.UtcNow,
                        IsSignature      = false
                    });
                    photoAdded = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(signatureData) && signatureData.StartsWith("data:image/png;base64,"))
            {
                try
                {
                    var base64 = signatureData["data:image/png;base64,".Length..];
                    var bytes = Convert.FromBase64String(base64);
                    var uniqueName = $"{Guid.NewGuid()}.png";
                    var savePath = Path.Combine(uploadDir, uniqueName);
                    await System.IO.File.WriteAllBytesAsync(savePath, bytes);
                    _context.ReturnDevicePhotos.Add(new ReturnDevicePhoto
                    {
                        ReturnDeviceId   = device.Id,
                        FilePath         = $"/uploads/{uniqueName}",
                        OriginalFileName = "signature.png",
                        UploadedAt       = DateTime.UtcNow,
                        IsSignature      = true
                    });
                    photoAdded = true;
                }
                catch (FormatException)
                {
                    // malformed base64 payload -- skip signature, device + damage photos still save
                }
            }

            if (photoAdded)
                await _context.SaveChangesAsync();

            if (addAnother == "true")
                return RedirectToAction(nameof(Create), new {
                    prefillLocation       = device.Location,
                    prefillDeviceType     = device.DeviceType,
                    prefillDeviceLocation = device.DeviceLocation,
                    prefillDate           = device.Date.ToString("yyyy-MM-dd"),
                    prefillRitm           = device.RITM,
                    prefillStatus         = statusChoice,
                    prefillWorkOrder      = device.WorkOrder
                });

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> BulkCreate(
            string[] serials, string[] types, string[] ritms,
            DateTime date, string deviceLocation, string status,
            string? workOrder, string? pickupStatus, string? location,
            string? chargerReturned, string? powerCableReturned,
            string? kid,
            string? damageStatus, string? damageComment,
            DateTime? leaseEndDatePlanned)
        {
            var count = Math.Min(serials?.Length ?? 0, Math.Min(types?.Length ?? 0, ritms?.Length ?? 0));
            bool? charger = string.IsNullOrEmpty(chargerReturned) ? null : bool.TryParse(chargerReturned, out var cr) ? cr : (bool?)null;
            bool? power   = string.IsNullOrEmpty(powerCableReturned) ? null : bool.TryParse(powerCableReturned, out var pcr) ? pcr : (bool?)null;

            // WIC Stock: create directly in WicStockDevices, redirect to WicStock index
            if (status == "WicStock")
            {
                for (int i = 0; i < count; i++)
                {
                    var sn = serials![i]?.Trim();
                    if (string.IsNullOrWhiteSpace(sn)) continue;
                    _context.WicStockDevices.Add(new WicStockDevice
                    {
                        SerialNumber    = sn,
                        DeviceType      = types![i]?.Trim() ?? "Lenovo",
                        DeviceStateType = "WIC Stock",
                        RITM            = ritms![i]?.Trim() ?? "",
                        Date            = date == default ? DateTime.Today : date,
                        DeviceLocation  = deviceLocation ?? "In WIC",
                        Status          = WicStockStatus.Available
                    });
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "WicStock");
            }

            var statusEnum = Enum.TryParse<ReturnDeviceStatus>(status, out var s) ? s : ReturnDeviceStatus.PendingPickup;

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
                    KID                = string.IsNullOrWhiteSpace(kid) ? null : kid,
                    DamageStatus       = string.IsNullOrWhiteSpace(damageStatus) ? null : damageStatus,
                    DamageComment      = string.IsNullOrWhiteSpace(damageComment) ? null : damageComment,
                    LeaseEndDatePlanned = leaseEndDatePlanned,
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
        public async Task<IActionResult> BulkEditSave(int[] ids, string? status, string? deviceLocation, string? workOrder, string? location, string? pickupStatus)
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
                if (!string.IsNullOrWhiteSpace(pickupStatus))
                    d.PickupStatus = pickupStatus;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var device = await _context.ReturnDevices
                .Include(d => d.Photos)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device == null) return NotFound();
            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ReturnDevice model, List<IFormFile>? photos, string? signatureData,
            string? statusChoice, List<IFormFile>? damagePhotos)
        {
            var device = await _context.ReturnDevices
                .Include(d => d.Photos)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device == null) return NotFound();

            // WIC Stock: move device before any normal update
            if (statusChoice == "WicStock")
            {
                _context.WicStockDevices.Add(new WicStockDevice
                {
                    SerialNumber   = device.SerialNumber,
                    DeviceType     = device.DeviceType,
                    DeviceStateType = "WIC Stock",
                    RITM           = device.RITM,
                    Date           = device.Date,
                    DeviceLocation = device.DeviceLocation,
                    Status         = WicStockStatus.Available
                });
                device.IsDeleted = true;
                device.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.Location)) ModelState.AddModelError("Location", "Location is required.");
            if (!ModelState.IsValid)
            {
                model.Photos = device.Photos;
                return View(model);
            }

            device.SerialNumber              = model.SerialNumber;
            device.DeviceType                = model.DeviceType;
            device.DeviceStateType           = "Return";
            device.RITM                      = model.RITM;
            device.Date                      = model.Date;
            device.DeviceLocation            = model.DeviceLocation;
            device.Status                    = Enum.TryParse<ReturnDeviceStatus>(statusChoice, out var sc) ? sc : device.Status;
            device.WorkOrder                 = model.WorkOrder;
            device.PickupStatus              = model.PickupStatus;
            device.Location                  = model.Location;
            device.KID                       = model.KID;
            device.ChargerReturned           = model.ChargerReturned;
            device.PowerCableReturned        = model.PowerCableReturned;
            device.DamageStatus              = model.DamageStatus;
            device.DamageComment             = model.DamageComment;
            device.LeaseEndDatePlanned       = model.LeaseEndDatePlanned;
            device.IsCustomerInducedDamage   = model.IsCustomerInducedDamage;
            device.IsBatterySwollen          = model.IsBatterySwollen;
            device.Acknowledgement           = model.Acknowledgement;

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadDir);

            var allowed = new[] { ".jpg", ".jpeg", ".png" };

            if (photos != null && photos.Count > 0)
            {
                foreach (var file in photos)
                {
                    if (file.Length == 0 || file.Length > 10 * 1024 * 1024) continue;
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowed.Contains(ext)) continue;

                    var uniqueName = $"{Guid.NewGuid()}{ext}";
                    var savePath = Path.Combine(uploadDir, uniqueName);
                    using var fs = new FileStream(savePath, FileMode.Create);
                    await file.CopyToAsync(fs);

                    device.Photos.Add(new LaptopTracker.Models.ReturnDevicePhoto
                    {
                        FilePath         = $"/uploads/{uniqueName}",
                        OriginalFileName = file.FileName,
                        UploadedAt       = DateTime.UtcNow,
                        IsSignature      = false
                    });
                }
            }

            if (damagePhotos != null && damagePhotos.Count > 0)
            {
                foreach (var file in damagePhotos)
                {
                    if (file.Length == 0 || file.Length > 10 * 1024 * 1024) continue;
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowed.Contains(ext)) continue;

                    var uniqueName = $"{Guid.NewGuid()}{ext}";
                    var savePath = Path.Combine(uploadDir, uniqueName);
                    using var fs = new FileStream(savePath, FileMode.Create);
                    await file.CopyToAsync(fs);

                    device.Photos.Add(new LaptopTracker.Models.ReturnDevicePhoto
                    {
                        FilePath         = $"/uploads/{uniqueName}",
                        OriginalFileName = $"damage-{file.FileName}",
                        UploadedAt       = DateTime.UtcNow,
                        IsSignature      = false
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(signatureData) && signatureData.StartsWith("data:image/png;base64,"))
            {
                try
                {
                    var base64 = signatureData["data:image/png;base64,".Length..];
                    var bytes = Convert.FromBase64String(base64);
                    var uniqueName = $"{Guid.NewGuid()}.png";
                    var savePath = Path.Combine(uploadDir, uniqueName);
                    await System.IO.File.WriteAllBytesAsync(savePath, bytes);

                    device.Photos.Add(new LaptopTracker.Models.ReturnDevicePhoto
                    {
                        FilePath         = $"/uploads/{uniqueName}",
                        OriginalFileName = "signature.png",
                        UploadedAt       = DateTime.UtcNow,
                        IsSignature      = true
                    });
                }
                catch (FormatException)
                {
                    // malformed base64 payload -- skip signature, device changes still save
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var photo = await _context.ReturnDevicePhotos.FindAsync(id);
            if (photo == null) return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photo.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.ReturnDevicePhotos.Remove(photo);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Photo(int id)
        {
            var photo = await _context.ReturnDevicePhotos.FindAsync(id);
            if (photo == null) return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photo.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            _context.AuditLogs.Add(new AuditLog
            {
                Kid        = _currentUser.Kid ?? "ANONYMOUS",
                Action     = "AttachmentView",
                EntityName = "ReturnDevicePhoto",
                EntityId   = id.ToString(),
                EntityLabel = photo.OriginalFileName ?? photo.FilePath,
                Source     = "Web",
                IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Route      = HttpContext.Request.Path.Value,
                RequestId  = HttpContext.TraceIdentifier
            });
            await _context.SaveChangesAsync();

            var ext = Path.GetExtension(photo.FilePath).ToLowerInvariant();
            var mime = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".gif"            => "image/gif",
                ".webp"           => "image/webp",
                _                 => "application/octet-stream"
            };
            var fileName = photo.OriginalFileName ?? Path.GetFileName(photo.FilePath);
            return PhysicalFile(fullPath, mime, fileName);
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

        // ── WP-12: Document hub ───────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Document(int id)
        {
            var device = await _context.ReturnDevices
                .Include(d => d.Photos)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device == null) return NotFound();

            _context.AuditLogs.Add(new AuditLog
            {
                Kid        = _currentUser.Kid ?? "ANONYMOUS",
                Action     = "Document",
                EntityName = nameof(ReturnDevice),
                EntityId   = id.ToString(),
                EntityLabel= device.SerialNumber,
                Source     = "Web",
                Route      = HttpContext.Request.Path.Value,
                IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Changes    = JsonSerializer.Serialize(new {
                    recipientKid     = device.KID,
                    includedSignature= device.Photos.Any(p => p.IsSignature),
                    format           = "pdf"
                })
            });
            await _context.SaveChangesAsync();

            return View(device);
        }

        [HttpGet]
        public async Task<IActionResult> DocumentPdf(int id)
        {
            var device = await _context.ReturnDevices
                .Include(d => d.Photos)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device == null) return NotFound();

            var bytes = _docSvc.GeneratePdf(device);
            var safeSn = string.Concat(device.SerialNumber
                .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            return File(bytes, "application/pdf", $"Rueckgabe_{safeSn}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> EmailDraft(int id)
        {
            var device = await _context.ReturnDevices
                .Include(d => d.Photos)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device == null) return NotFound();

            var pdfBytes = _docSvc.GeneratePdf(device);
            var pdfB64   = Convert.ToBase64String(pdfBytes);
            var safeSn   = string.Concat(device.SerialNumber
                .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            var pdfName  = $"Rueckgabe_{safeSn}.pdf";
            var toEmail  = _docSvc.ResolveEmail(device.KID);
            var subject  = $"Geräterückgabe – {device.SerialNumber}";
            var boundary = $"----=_Part_{Guid.NewGuid():N}";

            var body =
                $"Sehr geehrte/r Mitarbeiter/in,\r\n\r\n" +
                $"anbei erhalten Sie die Bestätigung der Geräterückgabe für " +
                $"Seriennummer {device.SerialNumber} (RITM: {device.RITM}).\r\n\r\n" +
                $"Mit freundlichen Grüßen\r\nGlobal Service Desk – WIC Team\r\n";

            // wrap base64 at 76 chars per RFC 2045
            var b64Lines = new System.Text.StringBuilder();
            for (int i = 0; i < pdfB64.Length; i += 76)
                b64Lines.AppendLine(pdfB64.Substring(i, Math.Min(76, pdfB64.Length - i)));

            var eml = new System.Text.StringBuilder();
            eml.AppendLine($"From: wic-tracker@eon.com");
            if (!string.IsNullOrEmpty(toEmail)) eml.AppendLine($"To: {toEmail}");
            eml.AppendLine($"Subject: {subject}");
            eml.AppendLine("MIME-Version: 1.0");
            eml.AppendLine("X-Unsent: 1");
            eml.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
            eml.AppendLine();
            eml.AppendLine($"--{boundary}");
            eml.AppendLine("Content-Type: text/plain; charset=UTF-8");
            eml.AppendLine("Content-Transfer-Encoding: 8bit");
            eml.AppendLine();
            eml.AppendLine(body);
            eml.AppendLine($"--{boundary}");
            eml.AppendLine($"Content-Type: application/pdf; name=\"{pdfName}\"");
            eml.AppendLine($"Content-Disposition: attachment; filename=\"{pdfName}\"");
            eml.AppendLine("Content-Transfer-Encoding: base64");
            eml.AppendLine();
            eml.Append(b64Lines);
            eml.AppendLine($"--{boundary}--");

            _context.AuditLogs.Add(new AuditLog
            {
                Kid        = _currentUser.Kid ?? "ANONYMOUS",
                Action     = "EmailDraft",
                EntityName = nameof(ReturnDevice),
                EntityId   = id.ToString(),
                EntityLabel= device.SerialNumber,
                Source     = "Web",
                Route      = HttpContext.Request.Path.Value,
                IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Changes    = JsonSerializer.Serialize(new { toEmail, pdfName })
            });
            await _context.SaveChangesAsync();

            var emlBytes = System.Text.Encoding.UTF8.GetBytes(eml.ToString());
            return File(emlBytes, "message/rfc822", $"Rueckgabe_{safeSn}.eml");
        }

        [HttpPost]
        public async Task<IActionResult> CaptureSignature(int id, string? signatureData)
        {
            var device = await _context.ReturnDevices.FindAsync(id);
            if (device == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(signatureData)
                && signatureData.StartsWith("data:image/png;base64,"))
            {
                try
                {
                    var base64 = signatureData["data:image/png;base64,".Length..];
                    var bytes  = Convert.FromBase64String(base64);
                    var name   = $"{Guid.NewGuid()}.png";
                    var path   = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", name);
                    await System.IO.File.WriteAllBytesAsync(path, bytes);
                    _context.ReturnDevicePhotos.Add(new ReturnDevicePhoto
                    {
                        ReturnDeviceId   = id,
                        FilePath         = $"/uploads/{name}",
                        OriginalFileName = "signature.png",
                        UploadedAt       = DateTime.UtcNow,
                        IsSignature      = true
                    });
                    await _context.SaveChangesAsync();
                }
                catch (FormatException) { }
            }
            return RedirectToAction(nameof(Document), new { id });
        }
    }
}






