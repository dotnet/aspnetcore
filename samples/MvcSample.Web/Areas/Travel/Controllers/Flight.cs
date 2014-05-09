using Microsoft.AspNet.Mvc;

namespace MvcSample.Web
{
    [Area("Travel")]
    public class Flight : Controller
    {
        public IActionResult Fly()
        {
            return View();
        }
    }
}
