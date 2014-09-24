using System;
using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Controllers.LinkGeneration
{
    [Route("api/orders/{id?}", Name = "OrdersApi")]
    public class OrdersController : Controller
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public IActionResult GetById(int id)
        {
            throw new NotImplementedException();
        }
    }
}