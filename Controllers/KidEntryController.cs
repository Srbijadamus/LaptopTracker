using Microsoft.AspNetCore.Mvc;
using LaptopTracker.Data;
using LaptopTracker.Models;

namespace LaptopTracker.Controllers
{
    public class KidEntryController : Controller
    {
        private readonly LaptopTrackerDbContext _context;
        public KidEntryController(LaptopTrackerDbContext context) => _context = context;

        [HttpGet("/KidEntry")]
        public IActionResult Index(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.CurrentKid = HttpContext.Session.GetString("kid");
            return View();
        }

        [HttpPost("/KidEntry")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string kid, string? returnUrl)
        {
            kid = (kid ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(kid))
            {
                ModelState.AddModelError("kid", "KID darf nicht leer sein.");
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.CurrentKid = HttpContext.Session.GetString("kid");
                return View();
            }

            HttpContext.Session.SetString("kid", kid);

            _context.AuditLogs.Add(new AuditLog
            {
                Kid       = kid,
                Action    = "Login",
                EntityName = "Session",
                EntityLabel = "KID entry",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Route     = "/KidEntry"
            });
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
