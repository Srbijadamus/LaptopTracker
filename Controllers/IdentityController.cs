using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaptopTracker.Data;
using LaptopTracker.Models;
using LaptopTracker.Services;

namespace LaptopTracker.Controllers
{
    [AllowAnonymous]
    public class IdentityController : Controller
    {
        private readonly LaptopTrackerDbContext _context;
        public IdentityController(LaptopTrackerDbContext context) => _context = context;

        [HttpGet("/Identity/Login")]
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.ReturnUrl   = returnUrl;
            ViewBag.CurrentKid  = HttpContext.Session.GetString(SessionCurrentUser.SessionKey);
            return View();
        }

        [HttpPost("/Identity/Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string kid, string? returnUrl)
        {
            kid = (kid ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(kid))
            {
                ModelState.AddModelError("kid", "KID darf nicht leer sein.");
                ViewBag.ReturnUrl  = returnUrl;
                ViewBag.CurrentKid = HttpContext.Session.GetString(SessionCurrentUser.SessionKey);
                return View();
            }

            HttpContext.Session.SetString(SessionCurrentUser.SessionKey, kid);

            _context.AuditLogs.Add(new AuditLog
            {
                Kid        = kid,
                Action     = "Login",
                EntityName = "Session",
                EntityLabel = "KID entry",
                IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Route      = "/Identity/Login"
            });
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost("/Identity/Logout")]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(SessionCurrentUser.SessionKey);
            return RedirectToAction("Login");
        }
    }
}
