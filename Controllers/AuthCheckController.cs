using System.Text.Json;
using LaptopTracker.Data;
using LaptopTracker.Models;
using LaptopTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaptopTracker.Controllers
{
    [Authorize]
    public class AuthCheckController : Controller
    {
        private readonly LaptopTrackerDbContext _context;
        private readonly ICurrentUser _currentUser;

        public AuthCheckController(LaptopTrackerDbContext context, ICurrentUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [HttpGet("/authcheck")]
        public async Task<IActionResult> Index()
        {
            var kid     = _currentUser.Kid;
            var rawName = User?.Identity?.Name;
            var authType = User?.Identity?.AuthenticationType;
            var isAuth  = User?.Identity?.IsAuthenticated;

            _context.AuditLogs.Add(new AuditLog
            {
                Kid        = kid ?? "ANONYMOUS",
                Action     = "AuthCheck",
                EntityName = "",
                EntityId   = "",
                Source     = "Web",
                Route      = "/authcheck",
                IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
                RequestId  = HttpContext.TraceIdentifier,
                Changes    = JsonSerializer.Serialize(new
                {
                    rawIdentity = rawName,
                    authType,
                    isAuth
                })
            });
            await _context.SaveChangesAsync();

            ViewBag.Kid      = kid;
            ViewBag.AuthType = authType;
            ViewBag.RawName  = rawName;
            return View();
        }
    }
}
