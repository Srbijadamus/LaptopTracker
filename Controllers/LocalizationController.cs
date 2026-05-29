using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace LaptopTracker.Controllers
{
    public class LocalizationController : Controller
    {
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
            );

            if (!Url.IsLocalUrl(returnUrl))
                return Redirect("/");

            return LocalRedirect(returnUrl);
        }
    }
}
