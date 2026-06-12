using Microsoft.AspNetCore.Mvc;

namespace LaptopTracker.Controllers
{
    public class CountryController : Controller
    {
        [HttpGet]
        public IActionResult Set(string code, string? returnUrl)
        {
            HttpContext.Session.SetString("CountryFilter", code ?? "");
            if (string.IsNullOrWhiteSpace(returnUrl))
                returnUrl = "/";
            return LocalRedirect(returnUrl);
        }
    }
}
