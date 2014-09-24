using System;
using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Controllers.LinkGeneration
{
    public class LinksController : Controller
    {
        public IActionResult Index(string view)
        {
            return View(viewName: view);
        }

        public string Details()
        {
            throw new NotImplementedException();
        }
    }
}
