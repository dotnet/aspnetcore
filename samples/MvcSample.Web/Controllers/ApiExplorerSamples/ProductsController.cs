using System;
using Microsoft.AspNet.Mvc;
using System.Collections.Generic;

namespace MvcSample.Web.ApiExplorerSamples
{
    [ApiExplorerSettings(GroupName = "Public API")]
    [Produces("application/json")]
    [Route("api/Products")]
    public class ProductsController : Controller
    {
        [HttpGet("{id}")]
        public Product GetById(int id)
        {
            return null;
        }

        [HttpGet("Search/{name}")]
        public IEnumerable<Product> SearchByName(string name)
        {
            return null;
        }

        [Produces("application/json", Type = typeof(ProductOrderConfirmation))]
        [HttpPut("{id}/Buy")]
        public IActionResult Buy(int projectId, int quantity = 1)
        {
            return null;
        }
    }
}