using LaptopTracker.Data;
using LaptopTracker.Models;
using LaptopTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopTracker.Controllers
{
    public class AuditController : Controller
    {
        private const int PageSize = 50;
        private readonly LaptopTrackerDbContext _context;
        private readonly ICurrentUser _currentUser;

        public AuditController(LaptopTrackerDbContext context, ICurrentUser currentUser)
        {
            _context     = context;
            _currentUser = currentUser;
        }

        public async Task<IActionResult> Index(
            string? kid,
            [FromQuery(Name = "action")] string? actionFilter,
            string? entityName,
            string? entityId, string? entityLabel, string? from, string? to,
            int page = 1)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Kid        = _currentUser.Kid ?? "ANONYMOUS",
                Action     = "AuditPageView",
                EntityName = "AuditLog",
                EntityId   = "",
                Source     = "Web",
                IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Route      = HttpContext.Request.Path.Value + HttpContext.Request.QueryString,
                RequestId  = HttpContext.TraceIdentifier
            });
            await _context.SaveChangesAsync();
            var q = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(kid))
                q = q.Where(a => a.Kid == kid);
            if (!string.IsNullOrWhiteSpace(actionFilter))
                q = q.Where(a => a.Action == actionFilter);
            if (!string.IsNullOrWhiteSpace(entityName))
                q = q.Where(a => a.EntityName == entityName);
            if (!string.IsNullOrWhiteSpace(entityId))
                q = q.Where(a => a.EntityId == entityId);
            if (!string.IsNullOrWhiteSpace(entityLabel))
                q = q.Where(a => a.EntityLabel != null && a.EntityLabel.Contains(entityLabel));
            if (DateTime.TryParse(from, out var dtFrom))
                q = q.Where(a => a.TimestampUtc >= dtFrom.ToUniversalTime());
            if (DateTime.TryParse(to, out var dtTo))
                q = q.Where(a => a.TimestampUtc < dtTo.ToUniversalTime().AddDays(1));

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(a => a.TimestampUtc)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["Kid"]         = kid;
            ViewData["Action"]      = actionFilter;
            ViewData["EntityName"]  = entityName;
            ViewData["EntityId"]    = entityId;
            ViewData["EntityLabel"] = entityLabel;
            ViewData["From"]        = from;
            ViewData["To"]          = to;
            ViewData["Page"]        = page;
            ViewData["TotalPages"]  = (int)Math.Ceiling((double)total / PageSize);
            ViewData["Total"]       = total;
            ViewData["AllActions"]  = await _context.AuditLogs
                .Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
            ViewData["AllEntities"] = await _context.AuditLogs
                .Where(a => a.EntityName != null && a.EntityName.Length > 0)
                .Select(a => a.EntityName).Distinct().OrderBy(a => a).ToListAsync();

            return View(items);
        }
    }
}
