using Microsoft.AspNet.Mvc;
using System;

namespace RoutingWebSite
{
    // This controller is reachable via traditional routing.
    [Area("Travel")]
    public class FlightController
    {
        private readonly TestResponseGenerator _generator;

        public FlightController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult Index()
        {
            return _generator.Generate("/Travel/Flight", "/Travel/Flight/Index");
        }

        [HttpPost]
        public IActionResult BuyTickets()
        {
            return _generator.Generate("/Travel/Flight/BuyTickets");
        }
    }
}