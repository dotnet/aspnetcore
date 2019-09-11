// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [ApiController]
    [Route("ApiExplorerResponseTypeWithApiConventionController/[Action]")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class ApiExplorerResponseTypeWithApiConventionController : Controller
    {
        [HttpGet]
        public Product GetProduct(int id) => null;

        [HttpGet]
        public Task<ActionResult<Product>> GetTaskOfActionResultOfProduct(int id) => null;

        [HttpGet]
        public IEnumerable<Product> GetProducts() => null;

        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType(202)]
        [ProducesResponseType(403)]
        public IActionResult PostWithConventions() => null;

        [HttpPost]
        [Produces("application/json", "text/json")]
        public IActionResult PostWithProduces(Product p) => null;

        [HttpPost]
        public Task<IActionResult> PostTaskOfProduct(Product p) => null;

        [HttpPut]
        public Task<IActionResult> Put(string id, Product product) => null;

        [HttpDelete]
        public Task<IActionResult> DeleteProductAsync(object id) => null;

        [HttpPost]
        [ApiConventionMethod(typeof(CustomConventions), nameof(CustomConventions.CustomConventionMethod))]
        public Task<IActionResult> PostItem(Product p) => null;
    }

    public static class CustomConventions
    {
        [ProducesResponseType(302)]
        [ProducesResponseType(409)]
        public static void CustomConventionMethod() { }
    }
}
