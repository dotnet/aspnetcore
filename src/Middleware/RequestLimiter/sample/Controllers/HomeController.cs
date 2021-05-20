using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RequestLimiter;

namespace RequestLimiterSample
{
    [RequestLimit(requestPerSecond: 10)]
    public class HomeController : Controller
    {
        [RequestLimit("rate")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
