using Microsoft.AspNetCore.Mvc;

namespace LaptopTracker.Controllers
{
    public class OperationsController : Controller
    {
        public IActionResult Index() => View();
    }
}
