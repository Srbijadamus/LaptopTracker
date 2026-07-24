using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LaptopTracker.Controllers
{
    public class HomeController : Controller
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? HttpContext.TraceIdentifier;
            ViewData["RequestId"] = requestId;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusCode(int? code)
        {
            ViewData["Code"] = code;
            return View();
        }
    }
}
