using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Mvc.RoutingWebSite.Controllers
{
    public class BranchesController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public BranchesController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult Index()
        {
            return _generator.Generate();
        }

        [HttpGet("dynamicattributeorder/{some}/{value}/{**slug}", Order = 1)]
        public IActionResult Attribute()
        {
            return _generator.Generate();
        }
    }
}
