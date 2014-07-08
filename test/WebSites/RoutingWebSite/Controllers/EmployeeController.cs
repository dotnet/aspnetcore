using Microsoft.AspNet.Mvc;
using System;

namespace RoutingWebSite
{
    // This controller combines routes on the controller with routes on actions in a REST + navigation property
    // style.
    [Route("api/Employee")]
    public class EmployeeController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public EmployeeController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult List()
        {
            return _generator.Generate("/api/Employee");
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return _generator.Generate("/api/Employee/" + id);
        }

        [HttpGet("{id}/Boss")]
        public IActionResult GetBoss(int id)
        {
            return _generator.Generate("/api/Employee/" + id + "/Boss");
        }

        [HttpGet("~/Manager/{id}")]
        public IActionResult GetManager(int id)
        {
            return _generator.Generate("/Manager/" + id);
        }
    }
}