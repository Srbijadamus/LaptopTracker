using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using LaptopTracker.Helpers;
using LaptopTracker.Models;

namespace LaptopTracker.Controllers
{
    public class WicStockController : Controller
    {
        private readonly LaptopTrackerDbContext _context;

        public WicStockController(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchQuery, string? statusFilter, string? deviceTypeFilter, string? siteFilter, string? categoryFilter)
        {
            var query = _context.WicStockDevices.Where(d => !d.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
                query = query.Where(d =>
                    d.SerialNumber.Contains(searchQuery) ||
                    d.RITM.Contains(searchQuery));

            if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<WicStockStatus>(statusFilter, out var sf))
                query = query.Where(d => d.Status == sf);

            // Real WIC site. The old Gerätestatus (DeviceLocation) filter is gone.
            if (!string.IsNullOrWhiteSpace(siteFilter))
                query = query.Where(d => d.Location == siteFilter);

            // Category is its own dimension, independent of state and status.
            if (!string.IsNullOrWhiteSpace(categoryFilter))
                query = query.Where(d => d.Category == categoryFilter);

            if (!string.IsNullOrWhiteSpace(deviceTypeFilter))
                query = query.Where(d => d.DeviceType == deviceTypeFilter);

            // Country filtering goes through the single existing session-based selector (sidebar ->
            // CountryController.Set -> Session["CountryFilter"]), same path every other list uses.
            // Must filter on the real site field (Location), not the DeviceLocation state field.
            var countryFilter = HttpContext.Session.GetString("CountryFilter");
            var locsInCountry = LaptopTracker.Helpers.LocationList.GetLocationsByCountry(countryFilter);
            if (locsInCountry != null)
                query = query.Where(d => d.Location != null && locsInCountry.Contains(d.Location));

            // Only sites actually present in the current Bestand; Location is nullable, so
            // rows with no site recorded contribute no option.
            var dbSites = await _context.WicStockDevices
                .Where(d => !d.IsDeleted && d.Location != null && d.Location.Length > 0)
                .Select(d => d.Location!)
                .Distinct()
                .ToListAsync();

            var dbDeviceTypes = await _context.WicStockDevices
                .Where(d => !d.IsDeleted)
                .Select(d => d.DeviceType)
                .Where(t => t != null && t.Length > 0)
                .Distinct()
                .ToListAsync();

            var knownTypes = new[] { "Lenovo", "HP", "Dell", "Apple iPad", "Desktop", "Rugged tablet", "Microsoft Surface tablet", "Apple tablet" };

            ViewData["DeviceTypes"]      = dbDeviceTypes.Concat(knownTypes).Distinct().OrderBy(t => t).ToList();
            ViewData["SearchQuery"]      = searchQuery;
            ViewData["StatusFilter"]     = statusFilter;
            ViewData["DeviceTypeFilter"] = deviceTypeFilter;
            ViewData["Sites"]            = dbSites.OrderBy(s => s).ToList();
            ViewData["SiteFilter"]       = siteFilter;
            ViewData["Categories"]       = WicCategories.ToList();
            ViewData["CategoryFilter"]   = categoryFilter;

            return View(await query.OrderByDescending(d => d.Date).ToListAsync());
        }

        private static readonly string[] WicStockStates = { "In WIC", "In Transit" };

        // Category values, validated like WicStockStates. NULL/empty means "no category".
        private static readonly string[] WicCategories = { "Re-imaged laptops" };

        // Mirrors ReturnDevicesController.CopyReturnFieldsToWic in the opposite direction:
        // carries the WIC form's shared Return-parity fields onto a Returns record. WIC-only
        // fields (Category, SwapRITM, availability, …) are deliberately not copied — they stay
        // on the WIC row, which is soft-deleted and restored by link when the device returns.
        private static void CopyWicFieldsToReturn(WicStockDevice source, ReturnDevice target)
        {
            target.Model                   = source.Model;
            target.Location                = source.Location;
            target.WorkOrder               = source.WorkOrder;
            target.PickupStatus            = source.PickupStatus;
            target.KID                     = source.KID;
            target.ChargerReturned         = source.ChargerReturned;
            target.PowerCableReturned      = source.PowerCableReturned;
            target.DamageStatus            = source.DamageStatus;
            target.DamageComment           = source.DamageComment;
            target.LeaseEndDatePlanned     = source.LeaseEndDatePlanned;
            target.IsCustomerInducedDamage = source.IsCustomerInducedDamage;
            target.IsBatterySwollen        = source.IsBatterySwollen;
            target.Acknowledgement         = source.Acknowledgement;
        }

        // Physical copies, not shared paths (same reason as CopyPhotosToWic on the Return side).
        private static void CopyPhotosToReturn(WicStockDevice source, ReturnDevice target)
        {
            if (source.Photos == null || source.Photos.Count == 0) return;

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadDir);

            foreach (var photo in source.Photos)
            {
                var sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    photo.FilePath.TrimStart('/'));
                if (!System.IO.File.Exists(sourcePath)) continue;

                var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FilePath)}";
                System.IO.File.Copy(sourcePath, Path.Combine(uploadDir, uniqueName));

                target.Photos.Add(new ReturnDevicePhoto
                {
                    FilePath         = $"/uploads/{uniqueName}",
                    OriginalFileName = photo.OriginalFileName,
                    UploadedAt       = photo.UploadedAt,
                    IsSignature      = photo.IsSignature
                });
            }
        }

        private async Task<List<DeviceModel>> GetActiveModelsAsync() =>
            await _context.Models.Where(m => m.IsActive).OrderBy(m => m.ModelName).ToListAsync();

        [HttpGet]
        public async Task<IActionResult> Create(string? serialNumber)
        {
            ViewData["Models"] = await GetActiveModelsAsync();
            return View(new WicStockDevice { Date = DateTime.Today, SerialNumber = serialNumber ?? string.Empty });
        }

        [HttpPost]
        public async Task<IActionResult> Create(WicStockDevice device)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Models"] = await GetActiveModelsAsync();
                return View(device);
            }
            device.DeviceStateType = "WIC Stock";
            _context.WicStockDevices.Add(device);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> BulkCreate(
            string[] serials, string[] types, string[] ritms,
            DateTime date, string deviceLocation, string status,
            string location, string model,
            string? statusChoice,
            string? swapRITM,
            string? category,
            string? workOrder, string? pickupStatus, string? kid,
            string? chargerReturned, string? powerCableReturned,
            string? damageStatus, string? damageComment,
            DateTime? leaseEndDatePlanned, string? acknowledgement,
            string? isCustomerInducedDamage, string? isBatterySwollen)
        {
            if (!LocationList.SiteLocations.Contains(location))
            {
                TempData["StatusMessage"] = "Site is required and must be selected from the list.";
                return RedirectToAction(nameof(Create));
            }
            var activeModelNames = (await GetActiveModelsAsync()).Select(m => m.ModelName).ToHashSet();
            if (!activeModelNames.Contains(model))
            {
                TempData["StatusMessage"] = "Model is required and must be selected from the list.";
                return RedirectToAction(nameof(Create));
            }

            var count = Math.Min(serials?.Length ?? 0, Math.Min(types?.Length ?? 0, ritms?.Length ?? 0));
            var statusEnum = Enum.TryParse<WicStockStatus>(status, out var s) ? s : WicStockStatus.Available;
            var state = WicStockStates.Contains(deviceLocation) ? deviceLocation : "In WIC";

            // Shared Return-parity settings, parsed the same way ReturnDevicesController.BulkCreate does.
            bool? charger = string.IsNullOrEmpty(chargerReturned) ? null : bool.TryParse(chargerReturned, out var cr) ? cr : (bool?)null;
            bool? power   = string.IsNullOrEmpty(powerCableReturned) ? null : bool.TryParse(powerCableReturned, out var pcr) ? pcr : (bool?)null;
            var cid       = bool.TryParse(isCustomerInducedDamage, out var cidv) && cidv;
            var battery   = bool.TryParse(isBatterySwollen, out var bsv) && bsv;
            var cat       = WicCategories.Contains(category ?? "") ? category : null;

            // Pending Pickup mirrors the Returns→WIC choice in the opposite direction: the device
            // is created directly as a Returns record and no WIC row is created. Site and Model are
            // already required above, matching the Returns create rules.
            if (statusChoice == "PendingPickup")
            {
                for (int i = 0; i < count; i++)
                {
                    var sn = serials![i]?.Trim();
                    if (string.IsNullOrWhiteSpace(sn)) continue;
                    _context.ReturnDevices.Add(new ReturnDevice
                    {
                        SerialNumber       = sn,
                        DeviceType         = types![i]?.Trim() ?? "Lenovo",
                        DeviceStateType    = "Return",
                        RITM               = ritms![i]?.Trim() ?? "",
                        Date               = date == default ? DateTime.Today : date,
                        DeviceLocation     = state,
                        Status             = ReturnDeviceStatus.PendingPickup,
                        Location           = location,
                        Model              = model,
                        WorkOrder          = string.IsNullOrWhiteSpace(workOrder) ? null : workOrder,
                        PickupStatus       = string.IsNullOrWhiteSpace(pickupStatus) ? null : pickupStatus,
                        KID                = string.IsNullOrWhiteSpace(kid) ? null : kid,
                        ChargerReturned    = charger,
                        PowerCableReturned = power,
                        DamageStatus       = string.IsNullOrWhiteSpace(damageStatus) ? null : damageStatus,
                        DamageComment      = string.IsNullOrWhiteSpace(damageComment) ? null : damageComment,
                        LeaseEndDatePlanned = leaseEndDatePlanned,
                        Acknowledgement    = string.IsNullOrWhiteSpace(acknowledgement) ? null : acknowledgement,
                        IsCustomerInducedDamage = cid,
                        IsBatterySwollen        = battery
                    });
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "ReturnDevices");
            }

            for (int i = 0; i < count; i++)
            {
                var sn = serials![i]?.Trim();
                if (string.IsNullOrWhiteSpace(sn)) continue;
                _context.WicStockDevices.Add(new WicStockDevice
                {
                    SerialNumber    = sn,
                    DeviceType      = types![i]?.Trim() ?? "Lenovo",
                    RITM            = ritms![i]?.Trim() ?? "",
                    Date            = date == default ? DateTime.Today : date,
                    DeviceLocation  = state,
                    Location        = location,
                    Model           = model,
                    Status          = statusEnum,
                    SwapRITM        = string.IsNullOrWhiteSpace(swapRITM) ? null : swapRITM,
                    DeviceStateType = "WIC Stock",
                    Category        = cat,
                    WorkOrder           = string.IsNullOrWhiteSpace(workOrder) ? null : workOrder,
                    PickupStatus        = string.IsNullOrWhiteSpace(pickupStatus) ? null : pickupStatus,
                    KID                 = string.IsNullOrWhiteSpace(kid) ? null : kid,
                    ChargerReturned     = charger,
                    PowerCableReturned  = power,
                    DamageStatus        = string.IsNullOrWhiteSpace(damageStatus) ? null : damageStatus,
                    DamageComment       = string.IsNullOrWhiteSpace(damageComment) ? null : damageComment,
                    LeaseEndDatePlanned = leaseEndDatePlanned,
                    Acknowledgement     = string.IsNullOrWhiteSpace(acknowledgement) ? null : acknowledgement,
                    IsCustomerInducedDamage = cid,
                    IsBatterySwollen        = battery
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
            var devices = await _context.WicStockDevices
                .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync();
            if (!devices.Any()) return RedirectToAction(nameof(Index));
            ViewData["Ids"] = ids;
            ViewData["Models"] = await GetActiveModelsAsync();
            return View(devices);
        }

        [HttpPost]
        [ActionName("BulkEdit")]
        public async Task<IActionResult> BulkEditSave(int[] ids, string? status, string? deviceLocation, string? location, string? model)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var idList = ids.ToList();
            var devices = await _context.WicStockDevices
                .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync();

            foreach (var d in devices)
            {
                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WicStockStatus>(status, out var s))
                    d.Status = s;
                if (!string.IsNullOrWhiteSpace(deviceLocation) && WicStockStates.Contains(deviceLocation))
                    d.DeviceLocation = deviceLocation;
                if (!string.IsNullOrWhiteSpace(location) && LocationList.SiteLocations.Contains(location))
                    d.Location = location;
                if (!string.IsNullOrWhiteSpace(model))
                    d.Model = model;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var device = await _context.WicStockDevices
                .Include(d => d.Photos)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device == null) return NotFound();
            ViewData["Models"] = await GetActiveModelsAsync();
            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, WicStockDevice input,
            List<IFormFile>? damagePhotos, string? signatureData, string? statusChoice = null)
        {
            var device = await _context.WicStockDevices
                .Include(d => d.Photos)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (device == null) return NotFound();

            // Transfer to Returns: "Pending Pickup" creates/restores the linked Returns row and
            // books the WIC row out (soft-delete). Site is required on the Returns side.
            if (statusChoice == "PendingPickup")
            {
                if (string.IsNullOrWhiteSpace(input.Location))
                    ModelState.AddModelError("Location", "Location is required.");
                if (!ModelState.IsValid)
                {
                    input.Photos = device.Photos;
                    ViewData["Models"] = await GetActiveModelsAsync();
                    return View(input);
                }

                // Find the linked Returns row by link only (soft-deleted or active) — never by serial.
                var returnDevice = device.ReturnDeviceId.HasValue
                    ? await _context.ReturnDevices.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(r => r.Id == device.ReturnDeviceId)
                    : null;

                input.LeaseEndDatePlanned = DateTimeHelper.MergeEditedDate(
                    device.LeaseEndDatePlanned, input.LeaseEndDatePlanned);

                if (returnDevice == null)
                {
                    returnDevice = new ReturnDevice
                    {
                        SerialNumber    = device.SerialNumber,
                        DeviceType      = device.DeviceType,
                        DeviceStateType = "Return",
                        RITM            = device.RITM,
                        Date            = device.Date,
                        DeviceLocation  = device.DeviceLocation,
                        Status          = ReturnDeviceStatus.PendingPickup
                    };
                    CopyWicFieldsToReturn(input, returnDevice);
                    CopyPhotosToReturn(device, returnDevice);
                    _context.ReturnDevices.Add(returnDevice);
                    device.ReturnDevice = returnDevice;
                }
                else
                {
                    // Restore the exact linked counterpart and refresh it from the WIC source.
                    returnDevice.IsDeleted = false;
                    returnDevice.DeletedAt = null;
                    returnDevice.SerialNumber    = device.SerialNumber;
                    returnDevice.DeviceType      = device.DeviceType;
                    returnDevice.DeviceStateType = "Return";
                    returnDevice.RITM            = device.RITM;
                    returnDevice.Date            = device.Date;
                    returnDevice.DeviceLocation  = device.DeviceLocation;
                    returnDevice.Status          = ReturnDeviceStatus.PendingPickup;
                    CopyWicFieldsToReturn(input, returnDevice);
                }

                device.IsDeleted = true;
                device.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "ReturnDevices");
            }

            if (!ModelState.IsValid)
            {
                input.Photos = device.Photos;
                ViewData["Models"] = await GetActiveModelsAsync();
                return View(input);
            }

            device.SerialNumber    = input.SerialNumber;
            device.DeviceType      = input.DeviceType;
            device.DeviceStateType = "WIC Stock";
            device.RITM            = input.RITM;
            device.Date            = input.Date;
            // DeviceLocation (Gerätestatus) is no longer edited through the UI; the stored
            // state value is preserved as-is so existing "In Transit"/"In WIC" rows are not reset.
            device.Location        = string.IsNullOrWhiteSpace(input.Location) ? null : input.Location;
            device.Model           = string.IsNullOrWhiteSpace(input.Model) ? null : input.Model;
            device.Status          = input.Status;
            device.SwapRITM        = input.SwapRITM;
            device.Category        = WicCategories.Contains(input.Category ?? "") ? input.Category : null;

            // Fields mirrored from the Return form.
            device.WorkOrder               = input.WorkOrder;
            device.PickupStatus            = input.PickupStatus;
            device.KID                     = input.KID;
            device.ChargerReturned         = input.ChargerReturned;
            device.PowerCableReturned      = input.PowerCableReturned;
            device.DamageStatus            = input.DamageStatus;
            device.DamageComment           = input.DamageComment;
            device.IsCustomerInducedDamage = input.IsCustomerInducedDamage;
            device.IsBatterySwollen        = input.IsBatterySwollen;
            device.Acknowledgement         = input.Acknowledgement;
            // Date-only edit: an untouched date keeps the stored value as it is.
            device.LeaseEndDatePlanned     = DateTimeHelper.MergeEditedDate(
                                                 device.LeaseEndDatePlanned, input.LeaseEndDatePlanned);

            await _context.SaveChangesAsync();
            await SaveAttachmentsAsync(device, damagePhotos, signatureData);
            return RedirectToAction(nameof(Index));
        }

        // Mirrors the Return form attachment handling: damage photos plus an optional
        // newly drawn signature. Existing signatures stay unless removed explicitly.
        private async Task SaveAttachmentsAsync(
            WicStockDevice device, List<IFormFile>? damagePhotos, string? signatureData)
        {
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadDir);

            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var added = false;

            if (damagePhotos != null && damagePhotos.Count > 0)
            {
                foreach (var file in damagePhotos)
                {
                    if (file.Length == 0 || file.Length > 10 * 1024 * 1024) continue;
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowed.Contains(ext)) continue;

                    var uniqueName = $"{Guid.NewGuid()}{ext}";
                    using var fs = new FileStream(Path.Combine(uploadDir, uniqueName), FileMode.Create);
                    await file.CopyToAsync(fs);
                    _context.WicStockDevicePhotos.Add(new WicStockDevicePhoto
                    {
                        WicStockDeviceId = device.Id,
                        FilePath         = $"/uploads/{uniqueName}",
                        OriginalFileName = $"damage-{file.FileName}",
                        UploadedAt       = DateTime.UtcNow,
                        IsSignature      = false
                    });
                    added = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(signatureData) && signatureData.StartsWith("data:image/png;base64,"))
            {
                try
                {
                    var bytes = Convert.FromBase64String(signatureData["data:image/png;base64,".Length..]);
                    var uniqueName = $"{Guid.NewGuid()}.png";
                    await System.IO.File.WriteAllBytesAsync(Path.Combine(uploadDir, uniqueName), bytes);
                    _context.WicStockDevicePhotos.Add(new WicStockDevicePhoto
                    {
                        WicStockDeviceId = device.Id,
                        FilePath         = $"/uploads/{uniqueName}",
                        OriginalFileName = "signature.png",
                        UploadedAt       = DateTime.UtcNow,
                        IsSignature      = true
                    });
                    added = true;
                }
                catch (FormatException)
                {
                    // malformed base64 payload -- skip signature, device changes still save
                }
            }

            if (added) await _context.SaveChangesAsync();
        }

        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var photo = await _context.WicStockDevicePhotos.FindAsync(id);
            if (photo == null) return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photo.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.WicStockDevicePhotos.Remove(photo);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Photo(int id)
        {
            var photo = await _context.WicStockDevicePhotos.FindAsync(id);
            if (photo == null) return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photo.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var ext = Path.GetExtension(photo.FilePath).ToLowerInvariant();
            var mime = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                _                 => "application/octet-stream"
            };
            return PhysicalFile(fullPath, mime, photo.OriginalFileName ?? Path.GetFileName(photo.FilePath));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await _context.WicStockDevices.FindAsync(id);
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
            var devices = await _context.WicStockDevices.Where(d => !d.IsDeleted).ToListAsync();
            foreach (var d in devices) { d.IsDeleted = true; d.DeletedAt = DateTime.UtcNow; }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Scan(string serialNumber, string? deviceLocation, string? status, string? swapRITM)
        {
            var sn = serialNumber.Trim().ToLower();
            var device = await _context.WicStockDevices
                .FirstOrDefaultAsync(d => d.SerialNumber.ToLower() == sn && !d.IsDeleted);

            if (device != null)
            {
                if (!string.IsNullOrWhiteSpace(deviceLocation)) device.DeviceLocation = deviceLocation;
                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WicStockStatus>(status, out var s)) device.Status = s;
                if (!string.IsNullOrWhiteSpace(swapRITM)) device.SwapRITM = swapRITM;
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
                if (await _context.WicStockDevices.AnyAsync(d => d.SerialNumber == sn)) continue;
                _context.WicStockDevices.Add(new WicStockDevice
                {
                    SerialNumber    = cols[0].Trim(),
                    DeviceType      = cols[1].Trim(),
                    DeviceStateType = cols[2].Trim(),
                    RITM            = cols[3].Trim(),
                    Date            = DateTime.TryParse(cols[4].Trim(), out var d) ? d : DateTime.UtcNow,
                    DeviceLocation  = cols[5].Trim(),
                    Status          = Enum.TryParse<WicStockStatus>(cols[6].Trim(), out var s) ? s : WicStockStatus.Available,
                    SwapRITM        = cols.Length > 7 ? cols[7].Trim() : null
                });
            }
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Import successful.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> CreateSwap(int id)
        {
            var device = await _context.WicStockDevices.FindAsync(id);
            if (device == null) return NotFound();
            ViewData["Models"] = await GetActiveModelsAsync();
            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSwap(int id, string swapSerialNumber, string swapRITM, DateTime swapDate, string swapDeviceType, string swapDeviceState, string swapLocation, string swapModel)
        {
            var oldDevice = await _context.WicStockDevices.FindAsync(id);
            if (oldDevice == null) return NotFound();

            if (!LocationList.SiteLocations.Contains(swapLocation))
            {
                TempData["StatusMessage"] = "Site is required and must be selected from the list.";
                return RedirectToAction(nameof(CreateSwap), new { id });
            }
            var activeModelNames = (await GetActiveModelsAsync()).Select(m => m.ModelName).ToHashSet();
            if (!activeModelNames.Contains(swapModel))
            {
                TempData["StatusMessage"] = "Model is required and must be selected from the list.";
                return RedirectToAction(nameof(CreateSwap), new { id });
            }

            // Mark old device as SwapPending
            oldDevice.SwapStatus       = "SwapPending";
            oldDevice.SwapSerialNumber = swapSerialNumber.Trim();
            oldDevice.SwapRITM         = swapRITM.Trim();
            oldDevice.SwapDate         = swapDate;

            // Add new device to WicStock as Available
            var newDevice = new WicStockDevice
            {
                SerialNumber    = swapSerialNumber.Trim(),
                DeviceType      = swapDeviceType.Trim(),
                RITM            = swapRITM.Trim(),
                Date            = swapDate,
                DeviceLocation  = WicStockStates.Contains(swapDeviceState) ? swapDeviceState : "In WIC",
                Location        = swapLocation,
                Model           = swapModel,
                Status          = WicStockStatus.Available,
                DeviceStateType = "WIC Stock"
            };
            _context.WicStockDevices.Add(newDevice);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateReturnFromSwap(int id)
        {
            var device = await _context.WicStockDevices.FindAsync(id);
            if (device == null) return NotFound();

            // Create ReturnDevice entry for defective device
            var returnDevice = new ReturnDevice
            {
                SerialNumber    = device.SerialNumber,
                DeviceType      = device.DeviceType,
                RITM            = device.RITM,
                Date            = DateTime.Today,
                DeviceLocation  = device.DeviceLocation,
                Status          = ReturnDeviceStatus.PendingPickup,
                PickupStatus    = "Swap Return Pending",
                DeviceStateType = "Return"
            };
            _context.ReturnDevices.Add(returnDevice);

            device.SwapStatus = "SwapReturnPending";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── Give directly to a user (KID) ─────────────────────────────────────
        // A re-imaged laptop can leave the WIC pool in one of two ways: the existing
        // "Geräte übergeben" handover (used for the AIOT path, Location="AIOT") or this
        // simpler single-device handover that records only the recipient's KID. Both
        // book the device out via the same soft-delete and create a Handover row, so the
        // Übergabeliste and the audit log stay the single evidence trail.

        [HttpGet]
        public async Task<IActionResult> GiveToUser(int id)
        {
            var device = await _context.WicStockDevices
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
            if (device == null) return NotFound();
            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> GiveToUser(int id, string kid, string? ritm)
        {
            var device = await _context.WicStockDevices
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (device == null)
            {
                TempData["StatusMessage"] = "Gerät nicht gefunden oder bereits übergeben.";
                return RedirectToAction(nameof(Index));
            }

            // Same KID rule as the login: trim + uppercase, reject empty. There is no
            // wider KID format in the system, so this is the whole validation.
            kid = (kid ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(kid))
            {
                TempData["StatusMessage"] = "KID ist erforderlich.";
                return RedirectToAction(nameof(GiveToUser), new { id });
            }

            if (device.Category != "Re-imaged laptops")
            {
                TempData["StatusMessage"] = "Nur re-image Laptops können direkt an einen Benutzer übergeben werden.";
                return RedirectToAction(nameof(GiveToUser), new { id });
            }

            if (device.DeviceLocation == "In Transit")
            {
                TempData["StatusMessage"] = "Das Gerät ist unterwegs und kann nicht direkt übergeben werden.";
                return RedirectToAction(nameof(GiveToUser), new { id });
            }

            // Record the recipient on the device row (visible in Archive), then book it
            // out via soft-delete — the same mechanism the WIC handover uses, so the
            // audit engine logs a SoftDelete and the device leaves the stock list.
            device.KID = kid;
            device.IsDeleted = true;
            device.DeletedAt = DateTime.UtcNow;

            // Standort = the device's real WIC site; Name = the device's serial (there is no
            // recipient name — the KID already has its own column). RITM goes into its own
            // HandoverDevice.Ritm field (distinct from the AIOT flow's WorkOrder).
            var site = string.IsNullOrWhiteSpace(device.Location) ? "Direkt an Benutzer" : device.Location;
            var ritmValue = string.IsNullOrWhiteSpace(ritm) ? device.RITM : ritm.Trim();

            var handover = new Handover
            {
                Name     = device.SerialNumber,
                Kid      = kid,
                Location = site,
                Date     = DateTime.Now,
                Devices = new List<HandoverDevice>
                {
                    new HandoverDevice
                    {
                        DeviceId     = device.Id,
                        DeviceType   = "WicStock",
                        SerialNumber = device.SerialNumber,
                        Ritm         = ritmValue
                    }
                }
            };
            _context.Handovers.Add(handover);

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Handovers", new { id = handover.Id });
        }
    }
}
