using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RequestLimiter;

namespace RequestLimiterSample
{
    [RequestLimit("policy1")]
    public class HomeController : Controller
    {
        [RequestLimit("policy2")] // concurrency1 then concurrency2
        public IActionResult Index()
        {
            return View();
        }

        [RequestLimit("policy3")]
        public IActionResult Index2()
        {
            return View();
        }
    }

    [RequestLimit("policy2")]
    public class Home2Controller : Controller
    {
        [RequestLimit("policy1")] // concurrency1 then concurrency2
        public IActionResult Index()
        {
            return View();
        }

        [RequestLimit("policy3")]
        public IActionResult Index2()
        {
            return View();
        }
    }
}
