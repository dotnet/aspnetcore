using Microsoft.AspNet.Mvc;

namespace AutofacWebSite.Controllers
{
    public class DIController : Controller
    {
        public DIController(HelloWorldBuilder builder)
        {
            Builder = builder;
        }

        public HelloWorldBuilder Builder { get; private set; }

        public IActionResult Index()
        {
            return View(model: Builder.Build());
        }
    }
}
