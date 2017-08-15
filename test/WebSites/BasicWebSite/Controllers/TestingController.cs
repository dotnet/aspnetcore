using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class TestingController
    {
        public TestingController(TestService service)
        {
            Service = service;
        }

        public TestService Service { get; }

        [HttpGet("Testing/Builder")]
        public string Get() => Service.Message;
    }
}
