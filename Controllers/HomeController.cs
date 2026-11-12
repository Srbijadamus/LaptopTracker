using System.Diagnostics;
using LaptopTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LaptopTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICurrentUser _currentUser;

        public HomeController(ILogger<HomeController> logger, ICurrentUser currentUser)
        {
            _logger = logger;
            _currentUser = currentUser;
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var path = feature?.Path;

            // Same id the error page shows, so a user's screenshot can be matched to a log line.
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            if (feature?.Error != null)
            {
                // Deliberately excludes request bodies and query strings; the path and method
                // are enough to locate the failing request without logging tokens/passwords.
                _logger.LogError(feature.Error,
                    "Unhandled exception processing {HttpMethod} {RequestPath} (RequestId={RequestId}, Kid={Kid})",
                    HttpContext.Request.Method, path, requestId, _currentUser.Kid ?? "ANONYMOUS");
            }

            ViewData["RequestId"] = requestId;
            ViewData["RequestPath"] = path;
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusCode(int? code)
        {
            ViewData["Code"] = code;
            return View();
        }
    }
}
