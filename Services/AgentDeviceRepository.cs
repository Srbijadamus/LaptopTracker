using LaptopTracker.Data;
using LaptopTracker.Models;
using LaptopTracker.Models.Agent;
using Microsoft.EntityFrameworkCore;

namespace LaptopTracker.Services
{
    public class AgentDeviceRepository : IAgentDeviceRepository
    {
        private readonly LaptopTrackerDbContext _context;

        private static readonly HashSet<string> _validSources = new(StringComparer.Ordinal)
        {
            "ReturnDevices", "LoanerDevices", "WicStock"
        };

        public AgentDeviceRepository(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        public bool IsValidSource(string source) => _validSources.Contains(source);

        // ── Mapping ─────────────────────────────────────────────────────────────

        private static DeviceDto Map(ReturnDevice d) => new()
        {
            Source          = "ReturnDevices",
            Id              = d.Id,
            SerialNumber    = d.SerialNumber,
            AssetTag        = null,
            DeviceType      = d.DeviceType,
            DeviceStateType = d.DeviceStateType,
            RITM            = d.RITM,
            Date            = d.Date,
            DeviceLocation  = d.DeviceLocation,
            Status          = d.Status.ToString(),
            IsDeleted       = d.IsDeleted,
            WorkOrder          = d.WorkOrder,
            PickupStatus       = d.PickupStatus,
            Location           = d.Location,
            ChargerReturned    = d.ChargerReturned,
            PowerCableReturned = d.PowerCableReturned
        };

        private static DeviceDto Map(LoanerDevice d) => new()
        {
            Source          = "LoanerDevices",
            Id              = d.Id,
            SerialNumber    = d.SerialNumber,
            AssetTag        = null,
            DeviceType      = d.DeviceType,
            DeviceStateType = d.DeviceStateType,
            RITM            = d.RITM,
            Date            = d.Date,
            DeviceLocation  = d.DeviceLocation,
            Status          = d.Status.ToString(),
            IsDeleted       = d.IsDeleted,
            KidHandedTo    = d.KidHandedTo,
            DateHandedOver = d.DateHandedOver,
            WIC            = d.WIC,
            KID            = d.KID
        };

        private static DeviceDto Map(WicStockDevice d) => new()
        {
            Source          = "WicStock",
            Id              = d.Id,
            SerialNumber    = d.SerialNumber,
            AssetTag        = null,
            DeviceType      = d.DeviceType,
            DeviceStateType = d.DeviceStateType,
            RITM            = d.RITM,
            Date            = d.Date,
            DeviceLocation  = d.DeviceLocation,
            Status          = d.Status.ToString(),
            IsDeleted       = d.IsDeleted,
            SwapRITM       = d.SwapRITM
        };

        // ── Read operations ──────────────────────────────────────────────────────

        public async Task<List<DeviceDto>> FindDeviceExactAsync(string identifier)
        {
            var sn = identifier.Trim().ToLower();
            var result = new List<DeviceDto>();

            var returns = await _context.ReturnDevices
                .Where(d => d.SerialNumber.ToLower() == sn)
                .ToListAsync();
            result.AddRange(returns.Select(Map));

            var loaners = await _context.LoanerDevices
                .Where(d => d.SerialNumber.ToLower() == sn)
                .ToListAsync();
            result.AddRange(loaners.Select(Map));

            var wic = await _context.WicStockDevices
                .Where(d => d.SerialNumber.ToLower() == sn)
                .ToListAsync();
            result.AddRange(wic.Select(Map));

            return result;
        }

        public async Task<List<DeviceDto>> FindDevicesByUserAsync(string userName)
        {
            var name = userName.Trim().ToLower();
            var loaners = await _context.LoanerDevices
                .Where(d => !d.IsDeleted &&
                    ((d.KidHandedTo != null && d.KidHandedTo.ToLower().Contains(name)) ||
                     (d.KID        != null && d.KID.ToLower().Contains(name))))
                .ToListAsync();
            return loaners.Select(Map).ToList();
        }

        public async Task<List<DeviceDto>> ListDevicesByStatusAsync(string status, string? source)
        {
            var src = NormalizeSource(source);
            var result = new List<DeviceDto>();

            if (src == null || src == "ReturnDevices")
            {
                if (Enum.TryParse<ReturnDeviceStatus>(status, out var rs))
                {
                    var items = await _context.ReturnDevices
                        .Where(d => !d.IsDeleted && d.Status == rs)
                        .ToListAsync();
                    result.AddRange(items.Select(Map));
                }
            }

            if (src == null || src == "LoanerDevices")
            {
                if (Enum.TryParse<LoanerStatus>(status, out var ls))
                {
                    var items = await _context.LoanerDevices
                        .Where(d => !d.IsDeleted && d.Status == ls)
                        .ToListAsync();
                    result.AddRange(items.Select(Map));
                }
            }

            if (src == null || src == "WicStock")
            {
                if (Enum.TryParse<WicStockStatus>(status, out var ws))
                {
                    var items = await _context.WicStockDevices
                        .Where(d => !d.IsDeleted && d.Status == ws)
                        .ToListAsync();
                    result.AddRange(items.Select(Map));
                }
            }

            return result;
        }

        public async Task<List<DeviceDto>> ListDevicesByLocationAsync(string location, string? source)
        {
            var src = NormalizeSource(source);
            var result = new List<DeviceDto>();

            if (src == null || src == "ReturnDevices")
            {
                var items = await _context.ReturnDevices
                    .Where(d => !d.IsDeleted &&
                        (d.DeviceLocation == location ||
                         (d.Location != null && d.Location == location)))
                    .ToListAsync();
                result.AddRange(items.Select(Map));
            }

            if (src == null || src == "LoanerDevices")
            {
                var items = await _context.LoanerDevices
                    .Where(d => !d.IsDeleted && d.DeviceLocation == location)
                    .ToListAsync();
                result.AddRange(items.Select(Map));
            }

            if (src == null || src == "WicStock")
            {
                var items = await _context.WicStockDevices
                    .Where(d => !d.IsDeleted && d.DeviceLocation == location)
                    .ToListAsync();
                result.AddRange(items.Select(Map));
            }

            return result;
        }

        public async Task<List<DeviceDto>> ListNotReturnedAsync(string? source)
        {
            var src = NormalizeSource(source);
            var result = new List<DeviceDto>();

            if (src == null || src == "LoanerDevices")
            {
                var items = await _context.LoanerDevices
                    .Where(d => !d.IsDeleted && d.Status == LoanerStatus.NotAvailable)
                    .ToListAsync();
                result.AddRange(items.Select(Map));
            }

            return result;
        }

        public async Task<List<DeviceDto>> ListPendingPickupsAsync(string? source)
        {
            var src = NormalizeSource(source);
            var result = new List<DeviceDto>();

            if (src == null || src == "ReturnDevices")
            {
                var items = await _context.ReturnDevices
                    .Where(d => !d.IsDeleted && d.Status == ReturnDeviceStatus.PendingPickup)
                    .ToListAsync();
                result.AddRange(items.Select(Map));
            }

            return result;
        }

        public async Task<List<InconsistencyDto>> DetectInconsistenciesAsync(string? source)
        {
            var src = NormalizeSource(source);
            var result = new List<InconsistencyDto>();

            // Load serial numbers from all three active sources for cross-source duplicate check
            var returnSns = await _context.ReturnDevices
                .Where(d => !d.IsDeleted).Select(d => d.SerialNumber).ToListAsync();
            var loanerSns = await _context.LoanerDevices
                .Where(d => !d.IsDeleted).Select(d => d.SerialNumber).ToListAsync();
            var wicSns = await _context.WicStockDevices
                .Where(d => !d.IsDeleted).Select(d => d.SerialNumber).ToListAsync();

            // Build serial-number → sources membership map
            var membership = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var sn in returnSns) AddMembership(membership, sn, "ReturnDevices");
            foreach (var sn in loanerSns) AddMembership(membership, sn, "LoanerDevices");
            foreach (var sn in wicSns)    AddMembership(membership, sn, "WicStock");

            foreach (var (sn, sources) in membership.Where(kvp => kvp.Value.Count > 1))
            {
                if (src != null && !sources.Contains(src)) continue;
                result.Add(new InconsistencyDto
                {
                    SerialNumber      = sn,
                    FoundInSources    = sources,
                    InconsistencyType = "DUPLICATE_SERIAL",
                    Detail            = $"Serial number '{sn}' exists in multiple sources: {string.Join(", ", sources)}"
                });
            }

            // LoanerDevice state inconsistencies
            if (src == null || src == "LoanerDevices")
            {
                var loaners = await _context.LoanerDevices.Where(d => !d.IsDeleted).ToListAsync();
                foreach (var d in loaners)
                {
                    if (d.Status == LoanerStatus.Available && !string.IsNullOrWhiteSpace(d.KidHandedTo))
                    {
                        result.Add(new InconsistencyDto
                        {
                            SerialNumber      = d.SerialNumber,
                            FoundInSources    = new List<string> { "LoanerDevices" },
                            InconsistencyType = "STATE_MISMATCH",
                            Detail            = $"LoanerDevice id={d.Id}: Status=Available but KidHandedTo='{d.KidHandedTo}'"
                        });
                    }
                    if (d.Status == LoanerStatus.NotAvailable && string.IsNullOrWhiteSpace(d.KidHandedTo))
                    {
                        result.Add(new InconsistencyDto
                        {
                            SerialNumber      = d.SerialNumber,
                            FoundInSources    = new List<string> { "LoanerDevices" },
                            InconsistencyType = "STATE_MISMATCH",
                            Detail            = $"LoanerDevice id={d.Id}: Status=NotAvailable but KidHandedTo is null/empty"
                        });
                    }
                }
            }

            return result;
        }

        // ── Write operations (target: exactly one record by source + id) ─────────

        public async Task<DeviceDto?> GetDeviceBySourceAndIdAsync(string source, int id)
        {
            return source switch
            {
                "ReturnDevices" => await _context.ReturnDevices
                    .Where(d => d.Id == id && !d.IsDeleted)
                    .Select(d => Map(d)).FirstOrDefaultAsync(),
                "LoanerDevices" => await _context.LoanerDevices
                    .Where(d => d.Id == id && !d.IsDeleted)
                    .Select(d => Map(d)).FirstOrDefaultAsync(),
                "WicStock" => await _context.WicStockDevices
                    .Where(d => d.Id == id && !d.IsDeleted)
                    .Select(d => Map(d)).FirstOrDefaultAsync(),
                _ => null
            };
        }

        public async Task<(bool ok, DeviceDto? device, string? error)> UpdateStatusAsync(
            string source, int id, string status)
        {
            switch (source)
            {
                case "ReturnDevices":
                    if (!Enum.TryParse<ReturnDeviceStatus>(status, out var rs))
                        return (false, null, $"Invalid status '{status}' for ReturnDevices. Valid: {string.Join(", ", Enum.GetNames<ReturnDeviceStatus>())}");
                    var rd = await _context.ReturnDevices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                    if (rd == null) return (false, null, $"ReturnDevice id={id} not found.");
                    rd.Status = rs;
                    await _context.SaveChangesAsync();
                    return (true, Map(rd), null);

                case "LoanerDevices":
                    if (!Enum.TryParse<LoanerStatus>(status, out var ls))
                        return (false, null, $"Invalid status '{status}' for LoanerDevices. Valid: {string.Join(", ", Enum.GetNames<LoanerStatus>())}");
                    var ld = await _context.LoanerDevices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                    if (ld == null) return (false, null, $"LoanerDevice id={id} not found.");
                    ld.Status = ls;
                    await _context.SaveChangesAsync();
                    return (true, Map(ld), null);

                case "WicStock":
                    if (!Enum.TryParse<WicStockStatus>(status, out var ws))
                        return (false, null, $"Invalid status '{status}' for WicStock. Valid: {string.Join(", ", Enum.GetNames<WicStockStatus>())}");
                    var wd = await _context.WicStockDevices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                    if (wd == null) return (false, null, $"WicStockDevice id={id} not found.");
                    wd.Status = ws;
                    await _context.SaveChangesAsync();
                    return (true, Map(wd), null);

                default:
                    return (false, null, $"Unknown source '{source}'.");
            }
        }

        public async Task<(bool ok, DeviceDto? device, string? error)> UpdateLocationAsync(
            string source, int id, string deviceLocation)
        {
            switch (source)
            {
                case "ReturnDevices":
                    var rd = await _context.ReturnDevices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                    if (rd == null) return (false, null, $"ReturnDevice id={id} not found.");
                    rd.DeviceLocation = deviceLocation;
                    await _context.SaveChangesAsync();
                    return (true, Map(rd), null);

                case "LoanerDevices":
                    var ld = await _context.LoanerDevices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                    if (ld == null) return (false, null, $"LoanerDevice id={id} not found.");
                    ld.DeviceLocation = deviceLocation;
                    await _context.SaveChangesAsync();
                    return (true, Map(ld), null);

                case "WicStock":
                    var wd = await _context.WicStockDevices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                    if (wd == null) return (false, null, $"WicStockDevice id={id} not found.");
                    wd.DeviceLocation = deviceLocation;
                    await _context.SaveChangesAsync();
                    return (true, Map(wd), null);

                default:
                    return (false, null, $"Unknown source '{source}'.");
            }
        }

        public async Task<(bool ok, DeviceDto? device, string? error)> UpdateAssignmentAsync(
            string source, int id, string? kidHandedTo, string? kid, DateTime? dateHandedOver)
        {
            if (source != "LoanerDevices")
                return (false, null, $"BLOCKED: Assignment updates are only supported for LoanerDevices. Source '{source}' does not have assignment fields.");

            var ld = await _context.LoanerDevices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
            if (ld == null) return (false, null, $"LoanerDevice id={id} not found.");

            ld.KidHandedTo    = kidHandedTo;
            ld.KID            = kid;
            ld.DateHandedOver = dateHandedOver;
            await _context.SaveChangesAsync();
            return (true, Map(ld), null);
        }

        public async Task<(bool ok, DeviceDto? device, string? error)> MarkReturnedAsync(string source, int id)
        {
            switch (source)
            {
                case "ReturnDevices":
                    var rd = await _context.ReturnDevices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                    if (rd == null) return (false, null, $"ReturnDevice id={id} not found.");
                    rd.Status = ReturnDeviceStatus.PickedUp;
                    await _context.SaveChangesAsync();
                    return (true, Map(rd), null);

                case "LoanerDevices":
                    var ld = await _context.LoanerDevices.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                    if (ld == null) return (false, null, $"LoanerDevice id={id} not found.");
                    ld.Status = LoanerStatus.Available;
                    await _context.SaveChangesAsync();
                    return (true, Map(ld), null);

                case "WicStock":
                    return (false, null, "BLOCKED: WicStockDevice does not have a return concept. Use status-update to change availability.");

                default:
                    return (false, null, $"Unknown source '{source}'.");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static string? NormalizeSource(string? source) =>
            string.IsNullOrEmpty(source) || source.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? null
                : source;

        private static void AddMembership(Dictionary<string, List<string>> map, string sn, string src)
        {
            if (!map.ContainsKey(sn)) map[sn] = new List<string>();
            if (!map[sn].Contains(src)) map[sn].Add(src);
        }
    }
}
