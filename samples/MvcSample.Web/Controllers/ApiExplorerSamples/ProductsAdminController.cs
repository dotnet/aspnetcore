using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;

namespace MvcSample.Web.ApiExplorerSamples
{
    [ApiExplorerSettings(GroupName = "Admin API")]
    [Route("api/Admin/Products")]
    public class ProductsAdminController : Controller
    {
        [HttpPut]
        public void AddProduct([FromBody] Product product)
        {
        }

        [HttpPost("{id}")]
        public void UpdateProduct([FromBody] Product product)
        {
        }

        [HttpPost("{id}/Stock")]
        public void SetQuantityInStock(int id, int quantity)
        {
        }

        [HttpPost("{id}/Price")]
        public void SetPrice(int id, decimal price)
        {
        }

        [Produces("application/json", "application/xml")]
        [HttpGet("{id}/Orders")]
        public IEnumerable<ProductOrderConfirmation> GetOrders(DateTime? fromData = null, DateTime? toDate = null)
        {
            return null;
        }
    }
}